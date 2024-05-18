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
            if (msg.MessageName == "UPLOAD_ARTIFACT")
            {
                string name = msg.ReadString();
                int flags = msg.ReadInt();

            }
            else if (msg.MessageName == "DOWNLOAD_ARTIFACT")
            {
                string name = msg.ReadString();

            }
            else if (msg.MessageName == "LIST_ARTIFACTS")
            {

            }
        }
    }
}
