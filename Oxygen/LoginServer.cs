using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class LoginServer : Node
    {
        private Users users = Users.Instance;

        public LoginServer()
            : base("LOGIN_SVR")
        {
        }

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            var msg = request.Message;
            var client = request.Client;
            string name = msg.MessageName;
            if (name == "LOGIN")
            {
                string username = msg.ReadString();
                byte[] password = msg.ReadByteArray();

                User? user = users.GetUserByName(username);

                if (user != null && User.CheckPassword(user, password))
                {
                    SendAck(request, msg.MessageName);

                    client.SetProperty("LOGGED_IN", true);
                    client.SetProperty("USER_NAME", username);

                    Audit.Instance.Log("User {0} logged in with username/password.", user.Name);
                }
                else
                {
                    SendNack(request, 200, "Login failed", msg.MessageName);
                    Audit.Instance.Log("Attempt to login for user {0} with an username/password failed.", username);
                }
            }
            else if (name == "LOGIN_API_KEY")
            {
                string apiKey = msg.ReadString();
                User? user = users.GetUserByAPIKey(apiKey);

                if (user != null)
                {
                    if (Authorizer.CheckPermission(user, this.Name, msg.MessageName))
                    {
                        SendAck(request, msg.MessageName);

                        client.SetProperty("LOGGED_IN", true);
                        client.SetProperty("USER_NAME", user.Name);

                        Audit.Instance.Log("User {0} logged in with API key.", user.Name);
                    }
                    else
                    {
                        SendNack(request, 200, "Login failed", msg.MessageName);
                        Audit.Instance.Log("User {0} attempted to login with a API key, but not permissioned.", user.Name);
                    }
                }
                else
                {
                    SendNack(request, 200, "Login failed", msg.MessageName);
                    Audit.Instance.Log("Attempt to login with an API key failed.");
                }
            }
            else if (name == "CREATE_API_KEY")
            {
                bool? loggedIn = (bool?)client.GetProperty("LOGGED_IN");
                string? username = client.GetProperty("USER_NAME") as string;
                if (loggedIn.GetValueOrDefault() && username != null)
                {
                    User? user = Users.Instance.GetUserByName(username);
                    if (user != null && Authorizer.CheckPermission(user, this.Name, msg.MessageName))
                    {
                        string apiKey = users.CreateAPIKey(username);

                        Message response = new Message(this.Name, msg.MessageName);
                        response.WriteString("ACK");
                        response.WriteString(apiKey);
						request.Send(response);
                    }
                    else
                    {
                        SendNack(request, 200, "API key could not be created.", msg.MessageName);
                        Audit.Instance.Log("User {0} attempted to create an API key, but not permissioned.", username);
                    }
                }
                else
                {
                    SendNack(request, 400, "Authorization required", msg.MessageName);
                    Audit.Instance.Log("Attempt to create an API key failed, user not logged in.");
                }
            }
            else if (name == "REVOKE_API_KEY")
            {
                bool? loggedIn = (bool?)client.GetProperty("LOGGED_IN");
                string? username = client.GetProperty("USER_NAME") as string;
                if (loggedIn.GetValueOrDefault() && username != null)
                {
                    User? user = Users.Instance.GetUserByName(username);
                    if (user != null && Authorizer.CheckPermission(user, this.Name, msg.MessageName))
                    {
                        users.RevokeAPIKey(username);

                        Message response = new Message(this.Name, msg.MessageName);
                        response.WriteString("ACK");
                        request.Send(response);
                    }
                    else
                    {
                        SendNack(request, 200, "API key could not be created.", msg.MessageName);
                        Audit.Instance.Log("User {0} attempted to create revokes API keys, but not permissioned.", username);
                    }
                }
                else
                {
                    SendNack(request, 400, "Authorization required", msg.MessageName);
                    Audit.Instance.Log("Attempt to revoke API keys failed, user not logged in.");
                }
            }
            else
            {
                SendNack(request, 100, "Invalid request", "CREATE_API_KEY");
            }
        }
    }
}
