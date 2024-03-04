using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oxygen
{
    public class ClientConnection
    {
        private Cache cache = new Cache();
        private TcpClient client;
        private NetworkStream networkStream;
        private List<Subscriber> subscribers = new List<Subscriber>();
        private object subscriberLock = new object();

        public ClientConnection(string hostname, int port)
        {
            client = new TcpClient(hostname, port);
            networkStream = client.GetStream();
        }

        public void LoadCache(string filename)
        {
            cache.LoadCache(filename);
        }

        public void SaveCache()
        {
            cache.SaveCache();
        }

        private void Send(byte[] bytes)
        {
            networkStream.WriteByte((byte)(bytes.Length & 0xFF));
            networkStream.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            networkStream.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            networkStream.WriteByte((byte)((bytes.Length >> 24) & 0xFF));

            networkStream.Write(bytes);
            networkStream.Flush();
        }

        private Message Read()
        {
            byte[] length = new byte[4];
            try
            {
                networkStream.Read(length, 0, length.Length);
            }
            catch (IOException ex)
            {
                throw new ClientException(0, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new ClientException(0, ex.Message);
            }

            int len = length[0] | (length[1] << 8) | (length[2] << 16) | (length[3] << 24);

            byte[] bytes = new byte[len];
            try
            {
                networkStream.Read(bytes, 0, len);
            }
            catch (IOException ex)
            {
                throw new ClientException(0, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new ClientException(0, ex.Message);
            }

            return new Message(bytes);
        }

        public void RunClientThread()
        {
            new Thread(() =>
            {
                while (true)
                {
                    Message msg = Read();
                    string? messageName = msg.MessageName;

                    Subscriber? subscriber = null;

                    lock (this.subscriberLock)
                    {
                        foreach (var sub in this.subscribers)
                        {
                            if (sub.NodeName == msg.NodeName &&
                                sub.MessageName == messageName)
                            {
                                subscriber = sub;
                                break;
                            }
                        }
                    }

                    if (subscriber != null)
                    {
                        subscriber.OnMessageRecieved(msg);
                    }
                }
            }).Start();
        }

        private void Subscriber_MessageReady(object? sender, Message e)
        {
            this.Send(e.GetData());
        }

        public void AddSubscriber(Subscriber subscriber)
        {
            lock (this.subscriberLock)
            {
                subscriber.MessageReady += Subscriber_MessageReady;
                this.subscribers.Add(subscriber);
            }
        }

        public void RemoveSubscriber(Subscriber subscriber)
        {
            lock (this.subscriberLock)
            {
                subscriber.MessageReady -= Subscriber_MessageReady;
                this.subscribers.Remove(subscriber);
            }
        }

        public IList<string> ListAssets()
        {
            return ListResource("ASSET_SVR", "ASSET_LIST");
        }

        public void DownloadAsset(string name)
        {
            BinaryWriter? fileWriter = null;

            SendDownloadAsset(name);

            Message response = Read();
            int recieved = 0;

            string ack = response.ReadString();

            int numBytes = 0;
            string? checksum;
            if (ack == "ACK")
            {
                checksum = response.ReadString();
                if (checksum != cache.GetChecksum(name))
                {
                    File.Delete(name);
                    fileWriter = new BinaryWriter(File.OpenWrite(name));

                    numBytes = response.ReadInt();

                    byte[] data = response.ReadByteArray();

                    recieved += data.Length;
                    fileWriter.Write(data);
                }
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMessage = response.ReadString();

                throw new ClientException(errorCode, errorMessage);
            }

            while (recieved < numBytes)
            {
                SendDownloadAssetPart();

                response = Read();

                ack = response.ReadString();
                if (ack == "ACK")
                {
                    byte[] data = response.ReadByteArray();

                    recieved += data.Length;
                    fileWriter?.Write(data);
                }
                else
                {
                    int errorCode = response.ReadInt();
                    string errorMessage = response.ReadString();

                    throw new ClientException(errorCode, errorMessage);
                }
            }

            fileWriter?.Close();

            if (checksum != null)
            {
                cache.CacheItem(name, checksum);
            }
        }

        private void SendDownloadAssetPart()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("DOWNLOAD_ASSET_PART");
                }

                Send(stream.ToArray());
            }
        }

        private void SendDownloadAsset(string name)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("DOWNLOAD_ASSET");
                    writer.Write(name);

                    string? checksum = cache.GetChecksum(name);
                    writer.Write(checksum != null ? 1 : 0);
                    if (checksum != null)
                    {
                        writer.Write(checksum);
                    }
                }

                Send(stream.ToArray());
            }
        }

        public void UploadAsset(string name)
        { 
            if (!File.Exists(name))
            {
                throw new ClientException(0, "File cannot be found");
            }

            FileStream file = File.OpenRead(name);

            const int chunkSize = 1024;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("UPLOAD_ASSET");
                    writer.Write(name);
                    writer.Write((int)file.Length);

                    byte[] data = new byte[chunkSize];
                    int size = file.Read(data, 0, data.Length);
                    writer.Write(size);
                    writer.Write(data, 0, size);
                }

                Send(stream.ToArray());
            }

            CheckAck();

            while (file.Position < file.Length)
            {
                byte[] data = new byte[chunkSize];
                int size = file.Read(data, 0, data.Length);
                UploadPart(data, size);

                CheckAck();
            }
        }

        private void CheckAck()
        {
            Message response = Read();

            string ack = response.ReadString();
            if (ack != "ACK")
            {
                int errorCode = response.ReadInt();
                string errorMessage = response.ReadString();

                throw new ClientException(errorCode, errorMessage);
            }
        }

        private void UploadPart(byte[] data, int size)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("UPLOAD_ASSET_PART");

                    writer.Write(size);
                    writer.Write(data, 0, size);

                    Send(stream.ToArray());
                }
            }
        }

        public void Login(string apikey)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("LOGIN_SVR");
                    writer.Write("LOGIN_API_KEY");

                    writer.Write(apikey);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        public void Login(string username, byte[] password)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("LOGIN_SVR");
                    writer.Write("LOGIN");

                    writer.Write(username);
                    writer.Write(password.Length);
                    writer.Write(password);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        public void CreateNewUser(string username, byte[] password)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("USER_SVR");
                    writer.Write("CREATE_USER");

                    writer.Write(username);
                    writer.Write(password.Length);
                    writer.Write(password);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        public void DeleteUser(string username)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("USER_SVR");
                    writer.Write("DELETE_USER");

                    writer.Write(username);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        private IList<string> ListResource(string node, string messageName, string? resource = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(node);
                    writer.Write(messageName);

                    if (resource != null)
                    {
                        writer.Write(resource);
                    }

                    Send(stream.ToArray());
                }
            }

            Message response = Read();
            List<string> resources = new List<string>();

            string ack = response.ReadString();
            if (ack == "ACK")
            {
                int numAssets = response.ReadInt();
                for (int i = 0; i < numAssets; i++)
                {
                    resources.Add(response.ReadString());
                }
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMsg = response.ReadString();

                throw new ClientException(errorCode, errorMsg);
            }

            return resources;
        }

        public IList<string> ListUsers()
        {
            return ListResource("USER_SVR", "USER_LIST");
        }

        public IList<string> GetPermissionsForUser(string username)
        {
            return ListResource("USER_SVR", "GET_PERMISSION", username);
        }

        public void RevokeAPIKeys(string username)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("LOGIN_SVR");
                    writer.Write("REVOKE_API_KEY");

                    writer.Write(username);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        public void SetPermission(string username, string nodeName, string messageName, string permission)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("USER_SVR");
                    writer.Write("SET_PERMISSION");

                    writer.Write(username);

                    int value = 0;
                    switch (permission)
                    {
                        case "allow": value = 0; break;
                        case "deny": value = 1; break;
                        case "default": value = 2; break;
                    }

                    writer.Write(value);
                    writer.Write(nodeName);
                    writer.Write(messageName);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        public string CreateAPIKey()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("LOGIN_SVR");
                    writer.Write("CREATE_API_KEY");

                    Send(stream.ToArray());
                }
            }

            Message response = Read();
            string apiKey = string.Empty;

            string ack = response.ReadString();
            if (ack == "ACK")
            {
                apiKey = response.ReadString();
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMsg = response.ReadString();
                throw new ClientException(errorCode, errorMsg);
            }

            return apiKey;
        }

        public void RestoreAsset(string assetName, int revision)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("ASSET_RESTORE");
                    writer.Write(assetName);
                    writer.Write(revision);

                    Send(stream.ToArray());
                }
            }

            CheckAck();
        }

        public string GetAssetHistory(string assetName)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("ASSET_HISTORY");
                    writer.Write(assetName);

                    Send(stream.ToArray());
                }
            }

            Message response = Read();
            string summary = string.Empty;

            string ack = response.ReadString();
            if (ack == "ACK")
            {
                summary = response.ReadString();
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMsg = response.ReadString();
                throw new ClientException(errorCode, errorMsg);
            }

            return summary;
        }
    }
}
