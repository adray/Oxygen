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
            Message msg = new Message("ASSET_SVR", "DOWNLOAD_ASSET_PART");
            Send(msg.GetData());
        }

        private void SendDownloadAsset(string name)
        {
            Message msg = new Message("ASSET_SVR", "DOWNLOAD_ASSET");
            msg.WriteString(name);
            string? checksum = cache.GetChecksum(name);
            msg.WriteInt(checksum != null ? 1 : 0);
            if (checksum != null)
            {
                msg.WriteString(checksum);
            }
            Send(msg.GetData());
        }

        public void UploadAsset(string name)
        { 
            if (!File.Exists(name))
            {
                throw new ClientException(0, "File cannot be found");
            }

            FileStream file = File.OpenRead(name);

            const int chunkSize = 1024;

            Message msg = new Message("ASSET_SVR", "UPLOAD_ASSET");
            msg.WriteString(name);
            msg.WriteInt((int)file.Length);
            byte[] data = new byte[chunkSize];
            int size = file.Read(data, 0, data.Length);
            msg.WriteInt(size);
            msg.WriteBytes(data, size);
            Send(msg.GetData());

            CheckAck();

            while (file.Position < file.Length)
            {
                data = new byte[chunkSize];
                size = file.Read(data, 0, data.Length);
                UploadPart(data, size);

                CheckAck();
            }
        }

        private Message CheckAck()
        {
            Message response = Read();

            string ack = response.ReadString();
            if (ack != "ACK")
            {
                int errorCode = response.ReadInt();
                string errorMessage = response.ReadString();

                throw new ClientException(errorCode, errorMessage);
            }

            return response;
        }

        private void UploadPart(byte[] data, int size)
        {
            Message msg = new Message("ASSET_SVR", "UPLOAD_ASSET_PART");
            msg.WriteInt(size);
            msg.WriteBytes(data, size);
            Send(msg.GetData());
        }

        public void Login(string apikey)
        {
            Message msg = new Message("LOGIN_SVR", "LOGIN_API_KEY");
            msg.WriteString(apikey);

            Send(msg.GetData());

            CheckAck();
        }

        public void Login(string username, byte[] password)
        {
            Message msg = new Message("LOGIN_SVR", "LOGIN");
            msg.WriteString(username);
            msg.WriteInt(password.Length);
            msg.WriteBytes(password);

            Send(msg.GetData());

            CheckAck();
        }

        public void ResetPassword(byte[] password, byte[] newPassword)
        {
            Message msg = new Message("USER_SVR", "RESET_PASSWORD");
            msg.WriteInt(password.Length);
            msg.WriteBytes(password);
            msg.WriteInt(newPassword.Length);
            msg.WriteBytes(newPassword);

            Send(msg.GetData());

            CheckAck();
        }

        public void CreateNewUser(string username, byte[] password)
        {
            Message msg = new Message("USER_SVR", "CREATE_USER");
            msg.WriteString(username);
            msg.WriteInt(password.Length);
            msg.WriteBytes(password);
            Send(msg.GetData());

            CheckAck();
        }

        public void DeleteUser(string username)
        {
            Message msg = new Message("USER_SVR", "DELETE_USER");
            msg.WriteString(username);
            Send(msg.GetData());

            CheckAck();
        }

        private IList<string> ListResource(string node, string messageName, string? resource = null)
        {
            Message msg = new Message(node, messageName);
            if (resource != null)
            {
                msg.WriteString(resource);
            }

            Send(msg.GetData());

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
            Message msg = new Message("LOGIN_SVR", "REVOKE_API_KEY");
            msg.WriteString(username);
            Send(msg.GetData());

            CheckAck();
        }

        public void SetPermission(string username, string nodeName, string messageName, string permission)
        {
            Message msg = new Message("USER_SVR", "SET_PERMISSION");
            msg.WriteString(username);

            int value = 0;
            switch (permission)
            {
                case "allow": value = 0; break;
                case "deny": value = 1; break;
                case "default": value = 2; break;
            }

            msg.WriteInt(value);
            msg.WriteString(nodeName);
            msg.WriteString(messageName);

            Send(msg.GetData());

            CheckAck();
        }

        public void SetGroupPermission(string group, string nodeName, string messageName, string permission)
        {
            Message msg = new Message("USER_SVR", "SET_GROUP_PERMISSION");
            msg.WriteString(group);

            int value = 0;
            switch (permission)
            {
                case "allow": value = 0; break;
                case "deny": value = 1; break;
                case "default": value = 2; break;
            }

            msg.WriteInt(value);
            msg.WriteString(nodeName);
            msg.WriteString(messageName);

            Send(msg.GetData());

            CheckAck();
        }

        public string CreateAPIKey()
        {
            Message msg = new Message("LOGIN_SVR", "CREATE_API_KEY");
            Send(msg.GetData());

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
            Message msg = new Message("ASSET_SVR", "ASSET_RESTORE");
            msg.WriteString(assetName);
            msg.WriteInt(revision);
            
            Send(msg.GetData());

            CheckAck();
        }

        public string GetAssetHistory(string assetName)
        {
            Message msg = new Message("ASSET_SVR", "ASSET_HISTORY");
            msg.WriteString(assetName);
            Send(msg.GetData());

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

        public void AddUserToGroup(string username, string group)
        {
            Message msg = new Message("USER_SVR", "ADD_USER_TO_GROUP");
            msg.WriteString(username);
            msg.WriteString(group);
            Send(msg.GetData());

            CheckAck();
        }

        public void RemoveUserFromGroup(string username, string group)
        {
            Message msg = new Message("USER_SVR", "REMOVE_USER_FROM_GROUP");
            msg.WriteString(username);
            msg.WriteString(group);
            Send(msg.GetData());

            CheckAck();
        }

        public List<string> ListUserGroups()
        {
            Message msg = new Message("USER_SVR", "USER_GROUPS_LIST");
            Send(msg.GetData());

            Message response = CheckAck();

            List<string> groups = new List<string>();
            int numGroups = response.ReadInt();
            for (int i = 0; i < numGroups; i++)
            {
                groups.Add(response.ReadString());
            }

            return groups;
        }

        public List<string> ListUsersInGroup(string group)
        {
            Message msg = new Message("USER_SVR", "USER_GROUP_INFO");
            msg.WriteString(group);
            Send(msg.GetData());

            Message response = CheckAck();

            List<string> users = new List<string>();
            int numUsers = response.ReadInt();
            for (int i = 0; i < numUsers; i++)
            {
                users.Add(response.ReadString());
            }

            return users;
        }

        public void CreateUserGroup(string name)
        {
            Message msg = new Message("USER_SVR", "CREATE_USER_GROUP");
            msg.WriteString(name);
            Send(msg.GetData());

            CheckAck();
        }

        public void DeleteUserGroup(string name)
        {
            Message msg = new Message("USER_SVR", "DELETE_USER_GROUP");
            msg.WriteString(name);
            Send(msg.GetData());

            CheckAck();
        }

        public void CreateAssetLabel(string name)
        {
            Message msg = new Message("ASSET_SVR", "CREATE_LABEL");
            msg.WriteString(name);
            Send(msg.GetData());

            CheckAck();
        }

        public List<string> GetAssetLabelSpec(string name)
        {
            Message msg = new Message("ASSET_SVR", "LABEL_SPEC");
            msg.WriteString(name);
            Send(msg.GetData());

            Message response = CheckAck();

            int numItems = response.ReadInt();
            List<string> assets = new List<string>();

            for (int i = 0; i < numItems; i++)
            {
                assets.Add(response.ReadString());
            }

            return assets;
        }

        public List<string> GetAssetLabels()
        {
			Message msg = new Message("ASSET_SVR", "LABEL_LIST");
			Send(msg.GetData());

			Message response = CheckAck();

			int numItems = response.ReadInt();
			List<string> assets = new List<string>();

			for (int i = 0; i < numItems; i++)
			{
				assets.Add(response.ReadString());
			}

			return assets;
		}
    }
}
