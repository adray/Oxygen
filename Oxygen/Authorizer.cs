using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Authorizer
    {
        public static bool IsAuthorized(Client client)
        {
            bool? loggedIn = (bool?)client.GetProperty("LOGGED_IN");
            if (!loggedIn.GetValueOrDefault())
            {
                SendNack(client, 400, "Authorization requried");
                Audit.Instance.Log("Failed attempt to access resource, user not logged in.");
                return false;
            }

            return true;
        }

        private static void SendNack(Client client, int errorCode, string msg)
        {
            Message response = new Message("", "");
            response.WriteString("NACK");
            response.WriteInt(errorCode);
            response.WriteString(msg);
            client.Send(response);
        }
    }
}
