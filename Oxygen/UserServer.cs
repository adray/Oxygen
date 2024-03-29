using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oxygen
{
    internal class UserServer : Node
    {
        private readonly Users users = Users.Instance;

        public UserServer() : base("USER_SVR")
        {
        }

        public override void OnRecieveMessage(Client client, Message msg)
        {
            base.OnRecieveMessage(client, msg);

            if (!Authorizer.IsAuthorized(client, msg))
            {
                return;
            }

            switch (msg.MessageName)
            {
                case "CREATE_USER":
                    {
                        CreateUser(client, msg);
                        break;
                    }

                case "DELETE_USER":
                    {
                        DeleteUser(client, msg);
                        break;
                    }

                case "RESET_PASSWORD":
                    {
                        ResetPassword(client, msg);
                        break;
                    }

                case "SET_PERMISSION":
                    {
                        SetPermission(client, msg);
                        break;
                    }

                case "GET_PERMISSION":
                    {
                        GetPermission(client, msg);
                        break;
                    }

                case "SET_GROUP_PERMISSION":
                    {
                        SetGroupPermission(client, msg);
                        break;
                    }

                case "USER_LIST":
                    {
                        UserList(client, msg);
                        break;
                    }

                case "CREATE_USER_GROUP":
                    {
                        CreateUserGroup(client, msg);
                        break;
                    }

                case "ADD_USER_TO_GROUP":
                    {
                        AddUserToGroup(client, msg);
                        break;
                    }

                case "REMOVE_USER_FROM_GROUP":
                    {
                        RemoveUserFromGroup(client, msg);
                        break;
                    }

                case "USER_GROUPS_LIST":
                    {
                        UserGroupsList(client, msg);
                        break;
                    }

                case "USER_GROUP_INFO":
                    {
                        UserGroupInfo(client, msg);
                        break;
                    }

                default:
                    SendNack(client, 100, $"Invalid request.", msg.MessageName);
                    break;
            }
        }

        private void CreateUserGroup(Client client, Message msg)
        {
            string name = msg.ReadString();

            UserGroup? group = users.CreateUserGroup(name);

            if (group != null)
            {
                SendAck(client, msg.MessageName);
            }
            else
            {
                SendNack(client, 100, "Unable to create user group.", msg.MessageName);
            }
        }

        private void UserGroupInfo(Client client, Message msg)
        {
            string name = msg.ReadString();

            var group = users.GetUserGroup(name);

            if (group != null)
            {
                Message response = new Message(msg.NodeName, msg.MessageName);
                response.WriteString("ACK");

                response.WriteInt(group.Users.Count);
                foreach (var user in group.Users)
                {
                    response.WriteString(user.Name);
                }

                client.Send(response);
            }
            else
            {
                this.SendNack(client, 100, "No such user group.", msg.MessageName);
            }
        }

        private void UserGroupsList(Client client, Message msg)
        {
            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var groups = users.UserGroupsList;

            response.WriteInt(groups.Count);
            foreach (var group in groups)
            {
                response.WriteString(group.Name);
            }

            client.Send(response);
        }

        private void RemoveUserFromGroup(Client client, Message msg)
        {
            string username = msg.ReadString();
            string userGroup = msg.ReadString();

            if (users.RemoveUserFromGroup(username, userGroup))
            {
                SendAck(client, msg.MessageName);
            }
            else
            {
                SendNack(client, 200, "Could not remove user from user group.", msg.MessageName);
            }
        }

        private void AddUserToGroup(Client client, Message msg)
        {
            string username = msg.ReadString();
            string userGroup = msg.ReadString();

            if (users.AddUserToGroup(username, userGroup))
            {
                SendAck(client, msg.MessageName);
            }
            else
            {
                SendNack(client, 200, "Could not add user to user group.", msg.MessageName);
            }
        }

        private void UserList(Client client, Message msg)
        {
            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var userList = users.UserList;
            response.WriteInt(userList.Count);
            foreach (User user in userList)
            {
                response.WriteString(user.Name);
            }

            client.Send(response);
        }

        private static void GetPermission(Client client, Message msg)
        {
            string user = msg.ReadString();

            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var permissions = Authorizer.GetPermissionsForUser(user);
            response.WriteInt(permissions.Count);
            foreach (string permission in permissions)
            {
                response.WriteString(permission);
            }

            client.Send(response);
        }

        private void SetGroupPermission(Client client, Message msg)
        {
            string groupName = msg.ReadString();
            int flags = msg.ReadInt();
            string node = msg.ReadString();
            string messageName = msg.ReadString();

            UserGroup? group = users.GetUserGroup(groupName);
            
            if (group != null)
            {
                if (flags == (int)Authorizer.PermissionAttribute.Default)
                {
                    Authorizer.RemovePermission(group, node, messageName);
                }
                else if (Authorizer.SetPermission(group, node, messageName, Authorizer.PermissionAttribute.Allow))
                {
                    Audit.Instance.Log("Permission {0}/{1} for user group {2} updated", node, messageName, groupName);
                    SendAck(client, msg.MessageName);
                }
                else
                {
                    SendNack(client, 200, $"Unable to set permission.", msg.MessageName);
                }
            }
            else
            {
                SendNack(client, 200, $"Cannot find user group {groupName}.", msg.MessageName);
            }
        }

        private void SetPermission(Client client, Message msg)
        {
            string userName = msg.ReadString();
            int flags = msg.ReadInt();
            string node = msg.ReadString();
            string messageName = msg.ReadString();

            User? user = users.GetUserByName(userName);

            if (user != null)
            {
                if (flags == (int)Authorizer.PermissionAttribute.Default)
                {
                    Authorizer.RemovePermission(user, node, messageName);
                    Audit.Instance.Log("Permission {0}/{1} for user {2} reverted", node, messageName, userName);
                    SendAck(client, msg.MessageName);
                }
                else if (Authorizer.SetPermission(user, node, messageName, (Authorizer.PermissionAttribute)flags))
                {
                    Audit.Instance.Log("Permission {0}/{1} for user {2} updated", node, messageName, userName);
                    SendAck(client, msg.MessageName);
                }
                else
                {
                    SendNack(client, 200, $"Unable to set permission.", msg.MessageName);
                }
            }
            else
            {
                SendNack(client, 200, $"Cannot find user {userName}.", msg.MessageName);
            }
        }

        private void ResetPassword(Client client, Message msg)
        {
            string? userName = client.GetProperty("USER_NAME") as string;
            if (userName != null)
            {
                User? user = users.GetUserByName(userName);

                if (user != null)
                {
                    byte[] password = msg.ReadByteArray();
                    byte[] newPassword = msg.ReadByteArray();
                    if (User.CheckPassword(user, password))
                    {
                        user.ChangePassword(newPassword);
                        users.WriteUserData();
                        Audit.Instance.Log("Password changed for user {0}.", userName);
                        SendAck(client, msg.MessageName);
                    }
                    else
                    {
                        SendNack(client, 100, "Unable to change password", msg.MessageName);
                    }
                }
                else
                {
                    SendNack(client, 200, $"Cannot find user {userName}.", msg.MessageName);
                }
            }
            else
            {
                SendNack(client, 200, $"Cannot find user {userName}.", msg.MessageName);
            }
        }

        private void DeleteUser(Client client, Message msg)
        {
            string userName = msg.ReadString();
            if (!string.IsNullOrEmpty(userName))
            {
                if (users.DeleteUser(userName))
                {
                    SendAck(client, msg.MessageName);
                }
                else
                {
                    SendNack(client, 200, $"Unable to delete user {userName}.", msg.MessageName);
                }
            }
            else
            {
                SendNack(client, 200, "Username not specified", msg.MessageName);
            }
        }

        private void CreateUser(Client client, Message msg)
        {
            string userName = msg.ReadString();
            byte[] password = msg.ReadByteArray();

            if (!string.IsNullOrEmpty(userName))
            {
                users.CreateUser(userName, password);
                SendAck(client, msg.MessageName);
            }
            else
            {
                SendNack(client, 200, "Username not specified", msg.MessageName);
            }
        }
    }
}
