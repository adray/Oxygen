using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class BuildServer : Node
    {
        private readonly ArtefactDataStream stream = new ArtefactDataStream();
        private const int DOWNLOAD_SIZE = 2048;

        public BuildServer() : base("BUILD_SVR")
        {
        }

        public override void OnClientDisconnected(Client client)
        {
            base.OnClientDisconnected(client);

            stream.CloseStreams(client);
        }

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            if (!Authorizer.IsAuthorized(request))
            {
                return;
            }

            var msg = request.Message;
            if (msg.MessageName == "ARTEFACT_DOWNLOAD_STREAM")
            {
                stream.ProcessDownloadStreamMessage(request);
            }
            else if (msg.MessageName == "ARTEFACT_UPLOAD_STREAM")
            {
                stream.ProcessUploadStreamMessage(request);
            }
            else if (msg.MessageName == "LIST_ARTEFACTS")
            {
                Message response = Response.Ack(this, msg.MessageName);

                string[] files = Directory.GetFiles("Artefacts");
                response.WriteInt(files.Length);
                foreach (string file in files)
                {
                    response.WriteString(Path.GetFileName(file));
                }

                request.Send(response);
            }
            else
            {
                SendNack(request, 100, "Invalid message type.", msg.MessageName);
            }
        }
    }
}
