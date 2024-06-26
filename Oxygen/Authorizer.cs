﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Authorizer
    {
        private const string permissionFile = "permissions.json";
        private const string authorizationData = @"Data\auth.data";
        private static readonly Dictionary<string, Permission> permissions = new Dictionary<string, Permission>();
        private static readonly Dictionary<string, PermissionAttribute> userPermissions = new Dictionary<string, PermissionAttribute>();
        private static readonly Dictionary<string, PermissionAttribute> groupPermissions = new Dictionary<string, PermissionAttribute>();

        private static void CreateDataDir()
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }
        }

        public static void LoadAuthorizationData()
        {
            CreateDataDir();

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

                        if (stream.Position < stream.Length)
                        {
                            numPermissions = reader.ReadInt32();
                            for (int i = 0; i < numPermissions; i++)
                            {
                                string key = reader.ReadString();
                                PermissionAttribute permission = (PermissionAttribute)reader.ReadInt32();

                                groupPermissions.Add(key, permission);
                            }
                        }
                    }
                }
            }
        }

        private static void SaveAuthorizationData()
        {
            CreateDataDir();

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

                    writer.Write(groupPermissions.Count);
                    foreach (var permission in groupPermissions)
                    {
                        writer.Write(permission.Key);
                        writer.Write((int)permission.Value);
                    }
                }
            }
        }

        public static bool SetPermission(UserGroup userGroup, string node, string message, PermissionAttribute permission)
        {
            if (permissions.TryGetValue(node + "." + message, out _))
            {
                string key = userGroup.Name + "." + node + "." + message;
                groupPermissions[key] = permission;
                SaveAuthorizationData();
                return true;
            }

            return false;
        }

        public static void RemovePermission(UserGroup userGroup, string node, string message)
        {
            string key = userGroup.Name + "." + node + "." + message;
            groupPermissions.Remove(key);
            SaveAuthorizationData();
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
            if (userPermissions.TryGetValue(key, out PermissionAttribute attribute) && attribute != PermissionAttribute.Default)
            {
                return attribute == PermissionAttribute.Allow ? true : false;
            }

            PermissionAttribute groupAttribute = PermissionAttribute.Default;
            foreach (var group in Users.Instance.GetUserGroups(user.Name))
            {
                string groupKey = group.Name + "." + node + "." + message;
                if (groupPermissions.TryGetValue(groupKey, out attribute) && attribute != PermissionAttribute.Default)
                {
                    if (groupAttribute == PermissionAttribute.Default)
                    {
                        groupAttribute = attribute;
                    }
                    else if (groupAttribute == PermissionAttribute.Allow &&
                        attribute == PermissionAttribute.Deny)
                    {
                        groupAttribute = PermissionAttribute.Deny;
                    }
                }
            }

            if (groupAttribute != PermissionAttribute.Default)
            {
                return groupAttribute == PermissionAttribute.Allow ? true : false;
            }

            if (permissions.TryGetValue(node + "." + message, out Permission? permission))
            {
                return permission.Attribute  == PermissionAttribute.Allow ? true : false;
            }

            return false;
        }

        public static IList<PermissionItem> GetPermissionsForUser(string username)
        {
            List<PermissionItem> permissionList = new List<PermissionItem>();
            IList<UserGroup> userGroups = Users.Instance.GetUserGroups(username);
            User? user = Users.Instance.GetUserByName(username);
            IList<UserGroup> myGroups = new List<UserGroup>();

            // Determine which groups contain the user.
            if (user != null)
            {
                foreach (var group in userGroups)
                {
                    if (group.Users.Contains(user))
                    {
                        myGroups.Add(group);
                    }
                }
            }

            string prefix = username + ".";

            foreach (var permission in permissions)
            {
                bool found = false;
                PermissionInherit inherited = PermissionInherit.Default;
                PermissionAttribute perm;

                // Look for the permission at the user level.
                if (userPermissions.TryGetValue(prefix + permission.Key, out perm) &&
                    perm != PermissionAttribute.Default)
                {
                    found = true;
                    inherited = PermissionInherit.User;
                }

                if (!found)
                {
                    // Look for the permission at the group level.
                    foreach (var group in myGroups)
                    {
                        string groupKey = group.Name + "." + permission.Key;
                        if (groupPermissions.TryGetValue(groupKey, out PermissionAttribute groupPerm))
                        {
                            if (found)
                            {
                                if (groupPerm == PermissionAttribute.Deny)
                                {
                                    perm = groupPerm;
                                    found = true;
                                    inherited = PermissionInherit.Group;
                                }
                                else if (groupPerm == PermissionAttribute.Allow)
                                {
                                    perm = groupPerm;
                                    found = true;
                                    inherited = PermissionInherit.Group;
                                }
                            }
                            else if (groupPerm != PermissionAttribute.Default)
                            {
                                perm = groupPerm;
                                found = true;
                                inherited = PermissionInherit.Group;
                            }
                        }
                    }
                }

                // Look for the default value of the permission.
                if (!found && permission.Value.Attribute != PermissionAttribute.Default)
                {
                    perm = permission.Value.Attribute;
                    found = true;
                }

                if (found)
                {
                    permissionList.Add(
                        new PermissionItem()
                        {
                            Attribute = perm,
                            Inherit = inherited,
                            MessageName = permission.Value.MessageName,
                            NodeName = permission.Value.NodeName
                        });
                }
            }

            return permissionList;
        }

        public static IList<Permission> GetAllPermissions()
        {
            List<Permission> list = new List<Permission>();
            foreach (var permission in permissions)
            {
                list.Add(permission.Value);
            }

            return list;
        }

        public static bool IsAuthorized(Request request)
        {
            var client = request.Client;
            var message = request.Message;

            bool? loggedIn = (bool?)client.GetProperty("LOGGED_IN");
            if (!loggedIn.GetValueOrDefault())
            {
                SendNack(request, 400, "Authorization requried", message);
                Audit.Instance.Log("Failed attempt to access resource, user not logged in.");
                return false;
            }

            string? username = client.GetProperty("USER_NAME") as string;
            if (username == null)
            {
                SendNack(request, 0, "Internal error", message);
                return false;
            }

            User? user = Users.Instance.GetUserByName(username);
            if (user == null)
            {
                SendNack(request, 0, "Internal error", message);
                return false;
            }

            if (!CheckPermission(user, message.NodeName, message.MessageName))
            {
                SendNack(request, 400, "Authorization requried", message);
                Audit.Instance.Log("Failed attempt to access resource {0}, user does not have the permission.", message.MessageName);
                return false;
            }

            return true;
        }

        private static void SendNack(Request request, int errorCode, string msg, Message message)
        {
            if (request.Message.MessageName.EndsWith("_STREAM"))
            {
                DataStreamBase.StatusError(request, msg);
            }
            else
            {
                request.Send(Response.Nack(message.NodeName, errorCode, msg, message.MessageName));
            }
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
                            permission.Attribute = Enum.Parse<PermissionAttribute>(reader.GetString() ?? "Allow");
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
