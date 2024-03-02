﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace O2Client
{
    public class Client
    {
        private TcpClient client;
        private NetworkStream networkStream;
        private List<Subscriber> subscribers = new List<Subscriber>();
        private object subscriberLock = new object();

        public Client(string hostname, int port)
        {
            client = new TcpClient(hostname, port);
            networkStream = client.GetStream();
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
            networkStream.Read(length, 0, length.Length);

            int len = length[0] | (length[1] << 8) | (length[2] << 16) | (length[3] << 24);

            byte[] bytes = new byte[len];
            networkStream.Read(bytes, 0, len);

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

        public List<string> ListAssets()
        {
            List<string> assets = new List<string>();

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write("ASSET_SVR");
                    writer.Write("ASSET_LIST");
                }

                Send(stream.ToArray());
            }

            Message response = Read();

            string ack = response.ReadString();
            if (ack == "ACK")
            {
                int numAssets = response.ReadInt();
                for (int i = 0; i < numAssets; i++)
                {
                    assets.Add(response.ReadString());
                }
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMsg = response.ReadString();

                throw new ClientException(errorCode, errorMsg);
            }

            return assets;
        }

        public void DownloadAsset(string name)
        {
            File.Delete(name);

            BinaryWriter fileWriter = new BinaryWriter(File.OpenWrite(name));

            SendDownloadAsset(name);

            Message response = Read();
            int recieved = 0;

            string ack = response.ReadString();

            int numBytes;
            if (ack == "ACK")
            {
                numBytes = response.ReadInt();

                byte[] data = response.ReadByteArray();

                recieved += data.Length;
                fileWriter.Write(data);
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
                    fileWriter.Write(data);
                }
                else
                {
                    int errorCode = response.ReadInt();
                    string errorMessage = response.ReadString();

                    throw new ClientException(errorCode, errorMessage);
                }
            }

            fileWriter.Close();
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