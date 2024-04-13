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

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            if (!Authorizer.IsAuthorized(request))
            {
                return;
            }

            var msg = request.Message;
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
            else if (msg.MessageName == "UPLOAD_CRASH_DUMP")
            {

            }
            else if (msg.MessageName == "DOWNLOAD_CRASH_DUMP")
            {

            }
        }
    }
}
