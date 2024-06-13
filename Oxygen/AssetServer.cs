using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class AssetServer : Node
    {
        private readonly Tags tagData = new Tags();
        private readonly DataStream stream = new DataStream();
        private readonly AssetDataStream dataStream;
        private readonly Cache cache = new Cache();

        public AssetServer() : base("ASSET_SVR")
        {
            this.dataStream = new AssetDataStream(this.cache);
            this.cache.LoadCache(@"Data\cache.data");
            Archiver.LoadAssetFile();
            AssetLabels.LoadLabelsFile();
        }

        public override void OnClientDisconnected(Client client)
        {
            base.OnClientDisconnected(client);

            this.stream.CloseUploadStream(client);
            this.stream.CloseDownloadStream(client);
        }

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            if (!Authorizer.IsAuthorized(request))
            {
                return;
            }

            var client = request.Client;
            var msg = request.Message;

            string? user = client.GetProperty("USER_NAME") as string;
            if (user == null)
            {
                SendNack(request, 0, "Internal error", msg.MessageName);
                return;
            }

            string msgName = msg.MessageName;
            if (msgName == "UPLOAD_ASSET")
            {
                string assetName = msg.ReadString();
                int sizeInBytes = msg.ReadInt();

                Archiver.BackupAsset(@"Assets\" + assetName);

                this.stream.AddUploadStream(client, @"Assets\" + assetName, sizeInBytes);
                Audit.Instance.Log("Asset {0} upload started by user {1}.", assetName, user);

                byte[] bytes = msg.ReadByteArray();
                if (this.stream.UploadBytes(client, bytes))
                {
                    this.cache.CacheItem(@"Assets\" + assetName);
                    this.cache.SaveCache();
                    Archiver.ArchiveAsset(@"Assets\" + assetName, user);
                    Audit.Instance.Log("Asset {0} upload finished by user {1}.", assetName, user);
                }

                SendAck(request, msgName);
            }
            else if (msgName == "UPLOAD_ASSET_PART")
            {
                string streamName = this.stream.GetUploadStreamName(client);
                byte[] bytes = msg.ReadByteArray();
                if (this.stream.UploadBytes(client, bytes))
                {
                    this.cache.CacheItem(streamName);
                    this.cache.SaveCache();
                    Archiver.ArchiveAsset(streamName, user);
                    Audit.Instance.Log("Asset {0} upload finished by user {1}.", streamName, user);
                }

                SendAck(request, msgName);
            }
            else if (msgName == "ASSET_UPLOAD_STREAM")
            {
                dataStream.ProcessUploadStreamMessage(request);
            }
            else if (msgName == "ASSET_DOWNLOAD_STREAM")
            {
                dataStream.ProcessDownloadStreamMessage(request);
            }
            else if (msgName == "ASSET_LIST")
            {
                Message response = new Message("ASSET_SVR", "ASSET_LIST");
                response.WriteString("ACK");

                var assets = Archiver.GetAssets();
                response.WriteInt(assets.Count);

                foreach (string asset in assets)
                {
                    response.WriteString(asset);
                }
                request.Send(response);
            }
            else if (msgName == "DOWNLOAD_ASSET")
            {
                string assetName = msg.ReadString();
                int hasChecksum = msg.ReadInt();
                string? checksum = this.cache.GetChecksum(@"Assets\" + assetName);
                if (string.IsNullOrEmpty(checksum))
                {
                    checksum = this.cache.CacheItem(@"Assets\" + assetName);
                    this.cache.SaveCache();
                }

                bool clientHasLatest = false;
                if (hasChecksum == 1)
                {
                    if (checksum == msg.ReadString())
                    {
                        clientHasLatest = true;
                    }
                }

                if (clientHasLatest)
                {
                    Message response = new Message("ASSET_SVR", "DOWNLOAD_ASSET");
                    response.WriteString("ACK");
                    response.WriteString(checksum);
                    request.Send(response);
                }
                else
                {
                    const int chunkSize = 1024;
                    int totalBytes = (int)this.stream.AddDownloadStream(client, @"Assets\" + assetName);
                    if (totalBytes > -1)
                    {
                        byte[]? bytes = this.stream.DownloadBytes(client, chunkSize);

                        if (bytes != null)
                        {
                            Message response = new Message("ASSET_SVR", "DOWNLOAD_ASSET");
                            response.WriteString("ACK");
                            response.WriteString(checksum);
                            response.WriteInt(totalBytes);
                            response.WriteInt(bytes.Length);
                            response.WriteBytes(bytes);
                            request.Send(response);

                            Audit.Instance.Log("Asset {0} download started by user {1}.", assetName, user);
                        }
                        else
                        {
                            SendNack(request, 200, "Download failed", msgName);
                        }
                    }
                    else
                    {
                        SendNack(request, 200, "Download already in progress", msgName);
                    }
                }
            }
            else if (msgName == "DOWNLOAD_ASSET_PART")
            {
                const int chunkSize = 1024;
                byte[]? bytes = this.stream.DownloadBytes(client, chunkSize);

                if (bytes != null)
                {
                    Message response = new Message("ASSET_SVR", "DOWNLOAD_ASSET_PART");
                    response.WriteString("ACK");
                    response.WriteInt(bytes.Length);
                    response.WriteBytes(bytes);
                    request.Send(response);
                }
                else
                {
                    SendNack(request, 200, "Download failed", msgName);
                }
            }
            else if (msgName == "ASSET_HISTORY")
            {
                string assetName = msg.ReadString();

                string summary = Archiver.GetAssetHistorySummary(assetName);

                Message response = new Message("ASSET_SVR", "ASSET_HISTORY");
                response.WriteString("ACK");
                response.WriteString(summary);
                request.Send(response);
            }
            else if (msgName == "ASSET_RESTORE")
            {
                string assetName = msg.ReadString();
                int revision = msg.ReadInt();

                if (Archiver.RestoreAsset($"Assets\\{assetName}", revision, user))
                {
                    SendAck(request, msgName);
                    Audit.Instance.Log("Asset {0} restored by user {1}.", assetName, user);
                }
                else
                {
                    SendNack(request, 200, "Unable to restore file at specified revision", msgName);
                }
            }
            else if (msgName == "CREATE_LABEL")
            {
                string name = msg.ReadString();

                bool ack = false;
                if (!AssetLabels.LabelExists(name))
                {
                    AssetLabel label = new AssetLabel(name, user);
                    IList<string> assets = Archiver.GetAssets();
                    foreach (var asset in assets)
                    {
                        label.AddItem(asset,
                            Archiver.GetAssetVersion(asset));
                    }

                    ack = AssetLabels.CreateLabel(label);
                }

                if (ack)
                {
                    SendAck(request, msgName);
                }
                else
                {
                    SendNack(request, 200, "Unable to create label, label with the given name already exists.", msgName);
                }
            }
            else if (msgName == "LABEL_SPEC")
            {
                string name = msg.ReadString();

                AssetLabel? label = null;
                if (AssetLabels.LabelExists(name))
                {
                    label = AssetLabels.FindLabel(name);
                }

                if (label != null)
                {
                    Message response = new Message(Name, msgName);
                    response.WriteString("ACK");

                    response.WriteInt(label.NumItems);
                    for (int i = 0; i < label.NumItems; i++)
                    {
                        response.WriteString(label.GetAssetName(i) + "," + label.GetAssetRevision(i));
                    }

                    request.Send(response);
                }
                else
                {
                    SendNack(request, 200, "Could not find the label specified.", msgName);
                }
            }
            else if (msgName == "LABEL_LIST")
            {
                var labels = AssetLabels.GetLabels();

                Message response = new Message(Name, msgName);
                response.WriteString("ACK");

                response.WriteInt(labels.Count);
                foreach(var label in labels)
                {
                    response.WriteString(label);
                }

                request.Send(response);
            }
            else if (msgName == "TAG_LIST")
            {
                string asset = msg.ReadString();
                var tags = tagData.QueryTagsByName(asset);
                Message response = Response.Ack(this.Name, msgName);
                if (tags != null)
                {
                    response.WriteInt(tags.Count);
                    foreach (var tag in tags)
                    {
                        response.WriteString(tag);
                    }
                }
                else
                {
                    response.WriteInt(0);
                }

                request.Send(response);
            }
            else if (msgName == "SEARCH_ASSETS")
            {
                string searchType = msg.ReadString();
                string searchText = msg.ReadString();

                if (searchType == "TAG")
                {
                    var assets = tagData.QueryTags(searchText);
                    Message response = Response.Ack(this.Name, msgName);
                    if (assets != null)
                    {
                        response.WriteInt(assets.Count);
                        foreach (string asset in assets)
                        {
                            response.WriteString(asset);
                        }
                    }
                    else
                    {
                        response.WriteInt(0);
                    }

                    request.Send(response);
                }
                else
                {
                    SendNack(request, 200, "Invalid search type specified.", msgName);
                }
            }
            else if (msgName == "ADD_ASSET_TAG")
            {
                var assetName = msg.ReadString();

                if (Archiver.GetAssetVersion(assetName) >= 0)
                {
                    int numTags = msg.ReadInt();
                    for (int i = 0; i < numTags; i++)
                    {
                        var tag = msg.ReadString();
                        tagData.AddNewTag(assetName, tag);
                    }

                    SendAck(request, msgName);
                }
                else
                {
                    SendNack(request, 200, "No such asset.", msgName);
                }
            }
            else if (msgName == "REMOVE_ASSET_TAG")
            {
                var assetName = msg.ReadString();
                int numTags = msg.ReadInt();
                for (int i = 0; i < numTags; i++)
                {
                    var tag = msg.ReadString();
                    tagData.DeleteTag(assetName, tag);
                }

                SendAck(request, msgName);
            }
            else
            {
                SendNack(request, 100, "Invalid request", msgName);
            }
        }
    }
}
