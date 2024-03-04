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
        private readonly List<string> assets = new List<string>();
        private readonly Cache cache = new Cache();

        public AssetServer() : base("ASSET_SVR")
        {
            this.LoadAssets();
            this.cache.LoadCache(@"Data\cache.data");
        }

        private void LoadAssets()
        {
            if (!Directory.Exists("Assets"))
            {
                Directory.CreateDirectory("Assets");
            }

            string[] files = Directory.GetFiles("Assets");
            foreach (string file in files)
            {
                assets.Add(Path.GetFileName(file));
            }
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

                this.assets.Add(assetName);
                this.stream.AddUploadStream(client, @"Assets\" + assetName, sizeInBytes);
                Audit.Instance.Log("Asset {0} upload started by user {1}.", assetName, user);

                byte[] bytes = msg.ReadByteArray();
                if (this.stream.UploadBytes(client, bytes))
                {
                    this.cache.CacheItem(@"Assets\" + assetName);
                    Archiver.ArchiveAsset(@"Assets\" + assetName);
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
                    Archiver.ArchiveAsset(streamName);
                    Audit.Instance.Log("Asset {0} upload finished by user {1}.", streamName, user);
                }

                SendAck(client, msgName);
            }
            else if (msgName == "ASSET_LIST")
            {
                Message response = new Message("ASSET_SVR", "ASSET_LIST");
                response.WriteString("ACK");
                response.WriteInt(this.assets.Count);

                foreach (string asset in this.assets)
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

                if (Archiver.RestoreAsset($"Assets\\{assetName}", revision))
                {
                    SendAck(client, msgName);
                    Audit.Instance.Log("Asset {0} restored by user {1}.", assetName, user);
                }
                else
                {
                    SendNack(client, 200, "Unable to restore file at specified revision", msgName);
                }

            }
            else
            {
                SendNack(client, 100, "Invalid request", msgName);
            }
        }
    }
}
