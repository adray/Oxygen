using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class LoginServer : Node
    {
        private Users users = new Users();

        public LoginServer()
            : base("LOGIN_SVR")
        {
            users.LoadUsers();
        }

        public override void OnRecieveMessage(Client client, Message msg)
        {
            base.OnRecieveMessage(client, msg);

            string name = msg.MessageName;
            if (name == "LOGIN")
            {
                string username = msg.ReadString();
                byte[] password = msg.ReadByteArray();

                User? user = users.GetUserByName(username);

                if (user != null && User.CheckPassword(user, password))
                {
                    SendAck(client, "LOGIN");

                    client.SetProperty("LOGGED_IN", true);
                    client.SetProperty("USER_NAME", username);

                    Audit.Instance.Log("User {0} logged in with username/password.", user.Name);
                }
                else
                {
                    SendNack(client, 200, "Login failed", "LOGIN");
                    Audit.Instance.Log("Attempt to login for user {0} with an username/password failed.", username);
                }
            }
            else if (name == "LOGIN_API_KEY")
            {
                string apiKey = msg.ReadString();
                User? user = users.GetUserByAPIKey(apiKey);

                if (user != null)
                {
                    SendAck(client, "LOGIN_API_KEY");

                    client.SetProperty("LOGGED_IN", true);
                    client.SetProperty("USER_NAME", user.Name);

                    Audit.Instance.Log("User {0} logged in with API key.", user.Name);
                }
                else
                {
                    SendNack(client, 200, "Login failed", "LOGIN_API_KEY");
                    Audit.Instance.Log("Attempt to login with an API key failed.");
                }
            }
            else if (name == "CREATE_API_KEY")
            {
                bool? loggedIn = (bool?)client.GetProperty("LOGGED_IN");
                string? username = client.GetProperty("USER_NAME") as string;
                if (loggedIn.GetValueOrDefault() && username != null)
                {
                    string apiKey = users.CreateAPIKey(username);

                    Message response = new Message(this.Name, "CREATE_API_KEY");
                    response.WriteString("ACK");
                    response.WriteString(apiKey);
                    client.Send(response);
                }
                else
                {
                    SendNack(client, 400, "Authorization required", "CREATE_API_KEY");
                    Audit.Instance.Log("Attempt to create an API key failed, user not logged in.");
                }
            }
            else
            {
                SendNack(client, 100, "Invalid request", "CREATE_API_KEY");
            }
        }
    }
}
