using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Authorizer
    {
        internal enum PermissionAttribute
        {
            Allow = 0,
            Deny = 1,
            Default = 2
        }

        private class Permission
        {
            public string NodeName { get; set; } = string.Empty;
            public string MessageName { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public PermissionAttribute DefaultPermission { get; set; }
        }

        private const string permissionFile = "permissions.json";
        private const string authorizationData = @"Data\auth.data";
        private static readonly Dictionary<string, Permission> permissions = new Dictionary<string, Permission>();
        private static readonly Dictionary<string, PermissionAttribute> userPermissions = new Dictionary<string, PermissionAttribute>();

        public static void LoadAuthorizationData()
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }

            if (File.Exists(authorizationData))
            {
                using (FileStream stream = File.OpenRead(authorizationData))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int numPermissions = reader.ReadInt32();
                        for (int i = 0; i < numPermissions; i++)
                        {
                            string key = reader.ReadString();
                            PermissionAttribute permission = (PermissionAttribute) reader.ReadInt32();

                            userPermissions.Add(key, permission);
                        }
                    }
                }
            }
        }

        private static void SaveAuthorizationData()
        {
            using (FileStream stream = File.OpenWrite(authorizationData))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(userPermissions.Count);
                    foreach (var permission in userPermissions)
                    {
                        writer.Write(permission.Key);
                        writer.Write((int)permission.Value);
                    }
                }
            }
        }
        
        public static bool SetPermission(User user, string node, string message, PermissionAttribute permission)
        {
            if (permissions.TryGetValue(node + "." + message, out _))
            {
                string key = user.Name + "." + node + "." + message;
                userPermissions[key] = permission;
                SaveAuthorizationData();
                return true;
            }

            return false;
        }

        public static void RemovePermission(User user, string node, string message)
        {
            string key = user.Name + "." + node + "." + message;
            userPermissions.Remove(key);
            SaveAuthorizationData();
        }

        public static bool CheckPermission(User user, string node, string message)
        {
            string key = user.Name + "." + node + "." + message;
            if (userPermissions.TryGetValue(key, out PermissionAttribute attribute))
            {
                return attribute == PermissionAttribute.Allow ? true : false;
            }

            if (permissions.TryGetValue(node + "." + message, out Permission? permission))
            {
                return permission.DefaultPermission  == PermissionAttribute.Allow ? true : false;
            }

            return false;
        }

        public static IList<string> GetPermissionsForUser(string username)
        {
            List<string> permissionList = new List<string>();
            string prefix = username + ".";
            foreach (var permission in userPermissions)
            {
                if (permission.Key.StartsWith(prefix))
                {
                    permissionList.Add($"{permission.Key} - {permission.Value}");
                }
            }
            return permissionList;
        }

        public static bool IsAuthorized(Client client, Message message)
        {
            bool? loggedIn = (bool?)client.GetProperty("LOGGED_IN");
            if (!loggedIn.GetValueOrDefault())
            {
                SendNack(client, 400, "Authorization requried", message);
                Audit.Instance.Log("Failed attempt to access resource, user not logged in.");
                return false;
            }

            string? username = client.GetProperty("USER_NAME") as string;
            if (username == null)
            {
                SendNack(client, 0, "Internal error", message);
                return false;
            }

            User? user = Users.Instance.GetUserByName(username);
            if (user == null)
            {
                SendNack(client, 0, "Internal error", message);
                return false;
            }

            if (!CheckPermission(user, message.NodeName, message.MessageName))
            {
                SendNack(client, 400, "Authorization requried", message);
                Audit.Instance.Log("Failed attempt to access resource, user does not have the permission.");
                return false;
            }

            return true;
        }

        private static void SendNack(Client client, int errorCode, string msg, Message message)
        {
            Message response = new Message(message.NodeName, message.MessageName);
            response.WriteString("NACK");
            response.WriteInt(errorCode);
            response.WriteString(msg);
            client.Send(response);
        }

        public static void LoadPermissions()
        {
            if (!File.Exists(permissionFile))
            {
                Logger.Instance.Log("Fatal error, unable to load {0}", permissionFile);
            }
            else
            {
                byte[] permissionText = File.ReadAllBytes(permissionFile);

                Utf8JsonReader reader = new Utf8JsonReader(permissionText);
                ParsePermissionFile(ref reader);
            }
        }

        private static void ParsePermissionFile(ref Utf8JsonReader reader)
        {
            bool success = true;

            while (success && reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        string? propertyName = reader.GetString()?.ToLower();
                        if (propertyName == "permissions")
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                success = ReadPermissionArray(ref reader);
                            }
                            else
                            {
                                success = false;
                            }
                        }
                        break;
                }
            }

            if (!success)
            {
                Logger.Instance.Log("Fatal error, unable to parse {0}.", permissionFile);
            }
        }

        private static bool ReadPermissionArray(ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                return false;
            }

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    return false;
                }

                if (!reader.Read())
                {
                    return false;
                }

                Permission permission = new Permission();

                while (reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        return false;
                    }

                    string? propertyName = reader.GetString()?.ToLower();

                    if (!reader.Read())
                    {
                        return false;
                    }

                    switch (propertyName)
                    {
                        case "node":
                            permission.NodeName = reader.GetString() ?? string.Empty;
                            break;
                        case "message":
                            permission.MessageName = reader.GetString() ?? string.Empty;
                            break;
                        case "text":
                            permission.Text = reader.GetString() ?? string.Empty;
                            break;
                        case "default":
                            permission.DefaultPermission = Enum.Parse<PermissionAttribute>(reader.GetString() ?? "Allow");
                            break;
                    }

                    reader.Read();
                }

                if (string.IsNullOrEmpty(permission.NodeName))
                {
                    Logger.Instance.Log("Unable to load permissions, 'NodeName' not specified.");
                    return false;
                }

                if (string.IsNullOrEmpty(permission.MessageName))
                {
                    Logger.Instance.Log("Unable to load permissions, 'MessageName' not specified.");
                    return false;
                }

                string key = permission.NodeName + "." + permission.MessageName;
                if (permissions.ContainsKey(key))
                {
                    Logger.Instance.Log("Unable to load permissions, duplicate key found {0}", key);
                    return false;
                }

                permissions.Add(key, permission);

                reader.Read();
            }

            return true;
        }
    }
}
