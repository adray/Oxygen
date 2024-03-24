using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class BuildServer : Node
    {
        public BuildServer() : base("BUILD_SVR")
        {
        }

        public override void OnRecieveMessage(Client client, Message msg)
        {
            base.OnRecieveMessage(client, msg);

            if (!Authorizer.IsAuthorized(client, msg))
            {
                return;
            }

            if (msg.MessageName == "UPLOAD_BUILD")
            {
                string name = msg.ReadString();

            }
            else if (msg.MessageName == "DOWNLOAD_BUILD")
            {
                string name = msg.ReadString();

            }
            else if (msg.MessageName == "LIST_BUILD")
            {

            }
        }
    }
}
