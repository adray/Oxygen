﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class BuildServer : Node
    {
        private readonly DataStream streams = new DataStream();
        private const int DOWNLOAD_SIZE = 2048;

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
            if (msg.MessageName == "UPLOAD_ARTEFACT")
            {
                string name = msg.ReadString();
                int flags = msg.ReadInt();

            }
            else if (msg.MessageName == "DOWNLOAD_ARTEFACT")
            {
                string name = msg.ReadString();

                string path = @"Artefacts\" + name;
                if (File.Exists(path))
                {
                    long totalBytes = streams.AddDownloadStream(request.Client, path);
                    if (totalBytes > 0)
                    {
                        byte[]? payload = streams.DownloadBytes(request.Client, DOWNLOAD_SIZE);
                        if (payload != null)
                        {
                            Message response = Response.Ack(this, msg.MessageName);
                            response.WriteInt((int)totalBytes);
                            response.WriteInt(payload.Length);
                            response.WriteBytes(payload);
                            request.Send(response);
                        }
                        else
                        {
                            streams.CloseDownloadStream(request.Client);
                            SendNack(request, 200, "Unable to download data.", msg.MessageName);
                        }
                    }
                    else
                    {
                        SendNack(request, 200, "Unable to start download stream.", msg.MessageName);
                    }
                }
                else
                {
                    SendNack(request, 200, "No such artefact exists.", msg.MessageName);
                }
            }
            else if (msg.MessageName == "DOWNLOAD_ARTEFACT_PART")
            {
                byte[]? payload = streams.DownloadBytes(request.Client, DOWNLOAD_SIZE);
                if (payload != null)
                {
                    Message response = Response.Ack(this, msg.MessageName);
                    response.WriteInt(payload.Length);
                    response.WriteBytes(payload);
                    request.Send(response);
                }
                else
                {
                    streams.CloseDownloadStream(request.Client);
                    SendNack(request, 200, "Unable to download data.", msg.MessageName);
                }
            }
            else if (msg.MessageName == "LIST_ARTEFACTS")
            {

            }
            else
            {
                SendNack(request, 100, "Invalid message type.", msg.MessageName);
            }
        }
    }
}
