﻿using System;
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

            if (msg.MessageName == "CREATE_USER")
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
            else if (msg.MessageName == "DELETE_USER")
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
            else if (msg.MessageName == "RESET_PASSWORD")
            {
                string? userName = client.GetProperty("USER_NAME") as string;
                if (userName != null)
                {
                    User? user = Users.Instance.GetUserByName(userName);

                    if (user != null)
                    {
                        byte[] password = msg.ReadByteArray();
                        byte[] newPassword = msg.ReadByteArray();
                        if (User.CheckPassword(user, password))
                        {
                            user.ChangePassword(newPassword);
                            users.WriteUserData();
                            Audit.Instance.Log("Password changed for user {0}.",  userName);
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
            else if (msg.MessageName == "SET_PERMISSION")
            {
                string userName = msg.ReadString();
                int flags = msg.ReadInt();
                string node = msg.ReadString();
                string messageName = msg.ReadString();

                User? user = Users.Instance.GetUserByName(userName);

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
            else if (msg.MessageName == "GET_PERMISSION")
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
            else if (msg.MessageName == "USER_LIST")
            {
                Message response = new Message(msg.NodeName, msg.MessageName);
                response.WriteString("ACK");

                var users = Users.Instance.UserList;
                response.WriteInt(users.Count);
                foreach (User user in users)
                {
                    response.WriteString(user.Name);
                }

                client.Send(response);
            }
            else if (msg.MessageName == "ADD_USER_TO_GROUP")
            {
                string username = msg.ReadString();
                string userGroup = msg.ReadString();

                if (Users.Instance.AddUserToGroup(username, userGroup))
                {
                    client.Send(Response.Ack(this, msg.MessageName));
                }
                else
                {
                    client.Send(Response.Nack(this, 200, "Could not add user to user group.", msg.MessageName));
                }
            }
            else
            {
                SendNack(client, 100, $"Invalid request.", msg.MessageName);
            }
        }
    }
}
