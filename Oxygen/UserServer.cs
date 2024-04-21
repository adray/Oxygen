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

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            if (!Authorizer.IsAuthorized(request))
            {
                return;
            }

            switch (request.Message.MessageName)
            {
                case "CREATE_USER":
                    {
                        CreateUser(request);
                        break;
                    }

                case "DELETE_USER":
                    {
                        DeleteUser(request);
                        break;
                    }

                case "RESET_PASSWORD":
                    {
                        ResetPassword(request);
                        break;
                    }

                case "SET_PERMISSION":
                    {
                        SetPermission(request);
                        break;
                    }

                case "GET_PERMISSION":
                    {
                        GetPermission(request);
                        break;
                    }

                case "GET_MY_PERMISSION":
                    {
                        GetMyPermission(request);
                        break;
                    }

                case "GET_ALL_PERMISSION":
                    {
                        GetAllPermissions(request);
                        break;
                    }

                case "SET_GROUP_PERMISSION":
                    {
                        SetGroupPermission(request);
                        break;
                    }

                case "USER_LIST":
                    {
                        UserList(request);
                        break;
                    }

                case "CREATE_USER_GROUP":
                    {
                        CreateUserGroup(request);
                        break;
                    }

                case "DELETE_USER_GROUP":
                    {
                        DeleteUserGroup(request);
                        break;
                    }

                case "ADD_USER_TO_GROUP":
                    {
                        AddUserToGroup(request);
                        break;
                    }

                case "REMOVE_USER_FROM_GROUP":
                    {
                        RemoveUserFromGroup(request);
                        break;
                    }

                case "USER_GROUPS_LIST":
                    {
                        UserGroupsList(request);
                        break;
                    }

                case "USER_GROUP_INFO":
                    {
                        UserGroupInfo(request);
                        break;
                    }

                default:
                    SendNack(request, 100, $"Invalid request.", request.Message.MessageName);
                    break;
            }
        }

        private void DeleteUserGroup(Request request)
        {
            var msg = request.Message;
            string name = msg.ReadString();

            if (users.DeleteUserGroup(name))
            {
                SendAck(request, msg.MessageName);
            }
            else
            {
                SendNack(request, 100, "Unable to delete user group.", msg.MessageName);
            }
        }

        private void CreateUserGroup(Request request)
        {
            var msg = request.Message;
            string name = msg.ReadString();

            UserGroup? group = users.CreateUserGroup(name);

            if (group != null)
            {
                SendAck(request, msg.MessageName);
            }
            else
            {
                SendNack(request, 100, "Unable to create user group.", msg.MessageName);
            }
        }

        private void UserGroupInfo(Request request)
        {
            var msg = request.Message;
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

                request.Send(response);
            }
            else
            {
                this.SendNack(request, 100, "No such user group.", msg.MessageName);
            }
        }

        private void UserGroupsList(Request request)
        {
            var msg = request.Message;
            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var groups = users.UserGroupsList;

            response.WriteInt(groups.Count);
            foreach (var group in groups)
            {
                response.WriteString(group.Name);
            }

            request.Send(response);
        }

        private void RemoveUserFromGroup(Request request)
        {
            var msg = request.Message;
            string username = msg.ReadString();
            string userGroup = msg.ReadString();

            if (users.RemoveUserFromGroup(username, userGroup))
            {
                SendAck(request, msg.MessageName);
            }
            else
            {
                SendNack(request, 200, "Could not remove user from user group.", msg.MessageName);
            }
        }

        private void AddUserToGroup(Request request)
        {
            var msg = request.Message;
            string username = msg.ReadString();
            string userGroup = msg.ReadString();

            if (users.AddUserToGroup(username, userGroup))
            {
                SendAck(request, msg.MessageName);
            }
            else
            {
                SendNack(request, 200, "Could not add user to user group.", msg.MessageName);
            }
        }

        private void UserList(Request request)
        {
            var msg = request.Message;
            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var userList = users.UserList;
            response.WriteInt(userList.Count);
            foreach (User user in userList)
            {
                response.WriteString(user.Name);
            }

            request.Send(response);
        }

        private static void GetAllPermissions(Request request)
        {
            var msg = request.Message;

            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var permissions = Authorizer.GetAllPermissions();
            response.WriteInt(permissions.Count);
            foreach (Permission permission in permissions)
            {
                response.WriteString(permission.NodeName);
                response.WriteString(permission.MessageName);
                response.WriteString(permission.Text);
                response.WriteInt((int)permission.Attribute);
            }

            request.Send(response);
        }

        private void GetMyPermission(Request request)
        {
            var msg = request.Message;

            string? user = request.Client.GetProperty("USER_NAME") as string;

            if (user != null)
            {
                Message response = new Message(msg.NodeName, msg.MessageName);
                response.WriteString("ACK");

                var permissions = Authorizer.GetPermissionsForUser(user);
                response.WriteInt(permissions.Count);
                foreach (PermissionItem permission in permissions)
                {
                    response.WriteString(permission.NodeName);
                    response.WriteString(permission.MessageName);
                    response.WriteInt((int)permission.Inherit);
                    response.WriteInt((int)permission.Attribute);
                }

                request.Send(response);
            }
            else
            {
                SendNack(request, 200, "No user defined.", msg.MessageName);
            }
        }

        private static void GetPermission(Request request)
        {
            var msg = request.Message;
            string user = msg.ReadString();

            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");

            var permissions = Authorizer.GetPermissionsForUser(user);
            response.WriteInt(permissions.Count);
            foreach (PermissionItem permission in permissions)
            {
                response.WriteString(permission.NodeName);
                response.WriteString(permission.MessageName);
                response.WriteInt((int)permission.Inherit);
                response.WriteInt((int)permission.Attribute);
            }

            request.Send(response);
        }

        private void SetGroupPermission(Request request)
        {
            var msg = request.Message;
            string groupName = msg.ReadString();
            int flags = msg.ReadInt();
            string node = msg.ReadString();
            string messageName = msg.ReadString();

            UserGroup? group = users.GetUserGroup(groupName);
            
            if (group != null)
            {
                if (flags == (int)PermissionAttribute.Default)
                {
                    Authorizer.RemovePermission(group, node, messageName);
                }
                else if (Authorizer.SetPermission(group, node, messageName, PermissionAttribute.Allow))
                {
                    Audit.Instance.Log("Permission {0}/{1} for user group {2} updated", node, messageName, groupName);
                    SendAck(request, msg.MessageName);
                }
                else
                {
                    SendNack(request, 200, $"Unable to set permission.", msg.MessageName);
                }
            }
            else
            {
                SendNack(request, 200, $"Cannot find user group {groupName}.", msg.MessageName);
            }
        }

        private void SetPermission(Request request)
        {
            var msg = request.Message;
            string userName = msg.ReadString();
            int flags = msg.ReadInt();
            string node = msg.ReadString();
            string messageName = msg.ReadString();

            User? user = users.GetUserByName(userName);

            if (user != null)
            {
                if (flags == (int)PermissionAttribute.Default)
                {
                    Authorizer.RemovePermission(user, node, messageName);
                    Audit.Instance.Log("Permission {0}/{1} for user {2} reverted", node, messageName, userName);
                    SendAck(request, msg.MessageName);
                }
                else if (Authorizer.SetPermission(user, node, messageName, (PermissionAttribute)flags))
                {
                    Audit.Instance.Log("Permission {0}/{1} for user {2} updated", node, messageName, userName);
                    SendAck(request, msg.MessageName);
                }
                else
                {
                    SendNack(request, 200, $"Unable to set permission.", msg.MessageName);
                }
            }
            else
            {
                SendNack(request, 200, $"Cannot find user {userName}.", msg.MessageName);
            }
        }

        private void ResetPassword(Request request)
        {
            var msg = request.Message;
            string? userName = request.Client.GetProperty("USER_NAME") as string;
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
                        SendAck(request, msg.MessageName);
                    }
                    else
                    {
                        SendNack(request, 100, "Unable to change password", msg.MessageName);
                    }
                }
                else
                {
                    SendNack(request, 200, $"Cannot find user {userName}.", msg.MessageName);
                }
            }
            else
            {
                SendNack(request, 200, $"Cannot find user {userName}.", msg.MessageName);
            }
        }

        private void DeleteUser(Request request)
        {
            var msg = request.Message;
            string userName = msg.ReadString();
            if (!string.IsNullOrEmpty(userName))
            {
                if (users.DeleteUser(userName))
                {
                    SendAck(request, msg.MessageName);
                }
                else
                {
                    SendNack(request, 200, $"Unable to delete user {userName}.", msg.MessageName);
                }
            }
            else
            {
                SendNack(request, 200, "Username not specified", msg.MessageName);
            }
        }

        private void CreateUser(Request request)
        {
            string userName = request.Message.ReadString();
            byte[] password = request.Message.ReadByteArray();

            if (!string.IsNullOrEmpty(userName))
            {
                User? user = users.CreateUser(userName, password);
                if (user != null)
                {
                    SendAck(request, request.Message.MessageName);
                }
                else
                {
                    SendNack(request, 200, "User could not be created", request.Message.MessageName);
                }
            }
            else
            {
                SendNack(request, 200, "Username not specified", request.Message.MessageName);
            }
        }
    }
}
