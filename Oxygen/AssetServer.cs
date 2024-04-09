using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class AssetServer : Node
    {
        private readonly DataStream stream = new DataStream();
        private readonly Cache cache = new Cache();

        public AssetServer() : base("ASSET_SVR")
        {
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

        public override void OnRecieveMessage(Client client, Message msg)
        {
            base.OnRecieveMessage(client, msg);

            if (!Authorizer.IsAuthorized(client, msg))
            {
                return;
            }

            string? user = client.GetProperty("USER_NAME") as string;
            if (user == null)
            {
                SendNack(client, 0, "Internal error", msg.MessageName);
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

                SendAck(client, msgName);
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

                SendAck(client, msgName);
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
                client.Send(response);
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
                    client.Send(response);
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
                            client.Send(response);

                            Audit.Instance.Log("Asset {0} download started by user {1}.", assetName, user);
                        }
                        else
                        {
                            SendNack(client, 200, "Download failed", msgName);
                        }
                    }
                    else
                    {
                        SendNack(client, 200, "Download already in progress", msgName);
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
                    client.Send(response);
                }
                else
                {
                    SendNack(client, 200, "Download failed", msgName);
                }
            }
            else if (msgName == "ASSET_HISTORY")
            {
                string assetName = msg.ReadString();

                string summary = Archiver.GetAssetHistorySummary(assetName);

                Message response = new Message("ASSET_SVR", "ASSET_HISTORY");
                response.WriteString("ACK");
                response.WriteString(summary);
                client.Send(response);
            }
            else if (msgName == "ASSET_RESTORE")
            {
                string assetName = msg.ReadString();
                int revision = msg.ReadInt();

                if (Archiver.RestoreAsset($"Assets\\{assetName}", revision, user))
                {
                    SendAck(client, msgName);
                    Audit.Instance.Log("Asset {0} restored by user {1}.", assetName, user);
                }
                else
                {
                    SendNack(client, 200, "Unable to restore file at specified revision", msgName);
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
                    SendAck(client, msgName);
                }
                else
                {
                    SendNack(client, 200, "Unable to create label, label with the given name already exists.", msgName);
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

                    client.Send(response);
                }
                else
                {
                    SendNack(client, 200, "Could not find the label specified.", msgName);
                }
            }
            else
            {
                SendNack(client, 100, "Invalid request", msgName);
            }
        }
    }
}
