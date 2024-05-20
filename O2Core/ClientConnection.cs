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
        //private List<Subscriber> subscribers = new List<Subscriber>();
        //private object subscriberLock = new object();
        private int messageId;

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

		public void Heartbeat()
		{
			Message msg = new Message("HEARTBEART", "");
			Send(msg.GetData());
		}

        private void Send(byte[] bytes)
        {
            networkStream.WriteByte((byte)(bytes.Length & 0xFF));
            networkStream.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            networkStream.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            networkStream.WriteByte((byte)((bytes.Length >> 24) & 0xFF));

            int id = messageId++;
            networkStream.WriteByte((byte)(id & 0xFF));
            networkStream.WriteByte((byte)((id >> 8) & 0xFF));
            networkStream.WriteByte((byte)((id >> 16) & 0xFF));
            networkStream.WriteByte((byte)((id >> 24) & 0xFF));

            networkStream.Write(bytes);
            networkStream.Flush();
        }

        private Message Read()
        {
            byte[] header = new byte[8];
            try
            {
                networkStream.ReadExactly(header, 0, header.Length);
            }
            catch (IOException ex)
            {
                throw new ClientException(0, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new ClientException(0, ex.Message);
            }

            int len = header[0] | (header[1] << 8) | (header[2] << 16) | (header[3] << 24);
            int id = header[4] | (header[5] << 8) | (header[6] << 16) | (header[7] << 24);

            byte[] bytes = new byte[len];
            try
            {
                networkStream.ReadExactly(bytes, 0, len);
            }
            catch (IOException ex)
            {
                throw new ClientException(0, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new ClientException(0, ex.Message);
            }

            Message msg = new Message(bytes);
            msg.Id = id;
            return msg;
        }

        //public void RunClientThread()
        //{
        //    new Thread(() =>
        //    {
        //        while (true)
        //        {
        //            Message msg = Read();
        //            string? messageName = msg.MessageName;

        //            Subscriber? subscriber = null;

        //            lock (this.subscriberLock)
        //            {
        //                foreach (var sub in this.subscribers)
        //                {
        //                    if (sub.NodeName == msg.NodeName &&
        //                        sub.MessageName == messageName)
        //                    {
        //                        subscriber = sub;
        //                        break;
        //                    }
        //                }
        //            }

        //            if (subscriber != null)
        //            {
        //                subscriber.OnMessageRecieved(msg);
        //            }
        //        }
        //    }).Start();
        //}

        //private void Subscriber_MessageReady(object? sender, Message e)
        //{
        //    this.Send(e.GetData());
        //}

        //public void AddSubscriber(Subscriber subscriber)
        //{
        //    lock (this.subscriberLock)
        //    {
        //        subscriber.MessageReady += Subscriber_MessageReady;
        //        this.subscribers.Add(subscriber);
        //    }
        //}

        //public void RemoveSubscriber(Subscriber subscriber)
        //{
        //    lock (this.subscriberLock)
        //    {
        //        subscriber.MessageReady -= Subscriber_MessageReady;
        //        this.subscribers.Remove(subscriber);
        //    }
        //}

        private void Download(IDownloadStream stream)
        {
            Message msg = stream.SendDownload();
            Send(msg.GetData());
            Message response = Read();
            stream.Download(response);
            while (!stream.Completed)
            {
                msg = stream.SendDownloadPart();
                Send(msg.GetData());
                response = Read();
                stream.DownloadPart(response);
            }
            stream.Close();
        }

        public void DownloadArtefact(string artefact)
        {
            Download(new ArtefactDownloadStream(artefact));
        }

        public void DownloadAsset(string asset)
        {
            Download(new AssetDownloadStream(asset, this.cache));
        }

        public IList<string> ListAssets()
        {
            return ListResource("ASSET_SVR", "ASSET_LIST");
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
                int numResources = response.ReadInt();
                for (int i = 0; i < numResources; i++)
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

        private IList<PermissionItem> GetPermissionsForUserInternal(string? username)
        {
            Message msg;
            if (username == null)
            {
                msg = new Message("USER_SVR", "GET_MY_PERMISSION");
            }
            else
            {
                msg = new Message("USER_SVR", "GET_PERMISSION");
                msg.WriteString(username);
            }

            Send(msg.GetData());

            Message response = Read();

            if (response.ReadString() == "ACK")
            {
                IList<PermissionItem> items = new List<PermissionItem>();

                int numResults = response.ReadInt();
                for (int i = 0; i < numResults; i++)
                {
                    string node = response.ReadString();
                    string message = response.ReadString();
                    int inherited = response.ReadInt();
                    PermissionAttribute attribute = (PermissionAttribute)response.ReadInt();

                    items.Add(new PermissionItem()
                    {
                        Attribute = attribute,
                        Inherit = (PermissionInherit)inherited,
                        MessageName = message,
                        NodeName = node
                    });
                }

                return items;
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMsg = response.ReadString();

                throw new ClientException(errorCode, errorMsg);
            }
        }

        public IList<PermissionItem> GetPermissionsForUser(string username)
        {
            return GetPermissionsForUserInternal(username);
        }

        public IList<PermissionItem> GetPermissionsForMySelf()
        {
            return GetPermissionsForUserInternal(null);
        }

        public IList<Permission> GetAllPermissions()
        {
            Message msg = new Message("USER_SVR", "GET_ALL_PERMISSION");

            Send(msg.GetData());

            Message response = Read();
            List<Permission> resources = new List<Permission>();

            string ack = response.ReadString();
            if (ack == "ACK")
            {
                int numResources = response.ReadInt();
                for (int i = 0; i < numResources; i++)
                {
                    Permission permission = new Permission();
                    permission.NodeName = response.ReadString();
                    permission.MessageName = response.ReadString();
                    permission.Text = response.ReadString();
                    permission.Attribute = (PermissionAttribute)response.ReadInt();

                    resources.Add(permission);
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

        public IList<string> SearchAssets(string searchType, string searchText)
        {
            Message msg = new Message("ASSET_SVR", "SEARCH_ASSETS");
            msg.WriteString(searchType);
            msg.WriteString(searchText);

            Send(msg.GetData());

            Message response = Read();
            List<string> resources = new List<string>();

            string ack = response.ReadString();
            if (ack == "ACK")
            {
                int numResources = response.ReadInt();
                for (int i = 0; i < numResources; i++)
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

        public void AddAssetTags(string asset, IList<string> tags)
        {
            Message msg = new Message("ASSET_SVR", "ADD_ASSET_TAG");
            msg.WriteString(asset);
            msg.WriteInt(tags.Count);
            for (int i = 0; i < tags.Count;i++)
            {
                msg.WriteString(tags[i]);
            }

            Send(msg.GetData());

            CheckAck();
        }

        public IList<string> GetTagsForAsset(string asset)
        {
            return ListResource("ASSET_SVR", "TAG_LIST", asset);
        }

        public IList<string> GetPlugins()
        {
            return ListResource("PLUGIN_SVR", "LIST_PLUGINS");
        }

        public IList<ScheduleItem> GetSchedule()
        {
            Message msg = new Message("PLUGIN_SVR", "SCHEDULE_LIST");
            Send(msg.GetData());

            var response = CheckAck();

            List<ScheduleItem> items = new List<ScheduleItem>();

            int num = response.ReadInt();
            for (int i = 0; i < num; i++)
            {
                bool running = response.ReadInt() == 1;
                string name = response.ReadString();
                bool startedManually = response.ReadInt() == 1;
                string startedBy = response.ReadString();
                string date = response.ReadString();

                items.Add(new ScheduleItem(running, name, date, startedBy, startedManually));
            }

            return items;
        }

        public IList<string> GetArtefacts()
        {
            return ListResource("BUILD_SVR", "LIST_ARTEFACTS");
        }
    }
}
