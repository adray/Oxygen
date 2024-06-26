﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Event
    {
        public virtual void HandleEvent(Node node)
        {
            // Do nothing
        }

        public virtual void EventEnd()
        {
            // Do nothing
        }
    }

    internal class ClientEvent : Event
    {
        public Client Client { get; private set; }

        public ClientEvent(Client client)
        {
            this.Client = client;
        }
    }

    internal class DisconnectionEvent : ClientEvent
    {
        public DisconnectionEvent(Client client) : base(client) { }

        public override void HandleEvent(Node node)
        {
            base.HandleEvent(node);

            this.Client.Disconnect();
            node.OnClientDisconnected(Client);
        }
    }

    internal class MessageRecievedEvent : ClientEvent
    {
        private Request request;
        private EventWaitHandle handle;
        private string name;

        public MessageRecievedEvent(Client client, string name, Request request, EventWaitHandle handle)
            : base(client)
        {
            this.request = request;
            this.name = name;
            this.handle = handle;
        }

        public override void HandleEvent(Node node)
        {
            base.HandleEvent(node);

            if (node.Name == name)
            {
                node.OnRecieveMessage(request);
            }
        }
    }

    internal class TimerEvent : Event
    {
        private NodeTimer timer;
        private Node node;

        public TimerEvent(NodeTimer timer, Node node)
        {
            this.timer = timer;
            this.node = node;
        }

        public override void EventEnd()
        {
            base.EventEnd();

            this.node.OnTimer(this.timer);
        }
    }
}
