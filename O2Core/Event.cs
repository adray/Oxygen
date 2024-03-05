using System;
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

            node.OnClientDisconnected(Client);
        }
    }

    internal class MessageRecievedEvent : ClientEvent
    {
        private Message msg;
        private EventWaitHandle handle;
        private string name;

        public MessageRecievedEvent(Client client, string name, Message msg, EventWaitHandle handle)
            : base(client)
        {
            this.msg = msg;
            this.name = name;
            this.handle = handle;
        }

        public override void HandleEvent(Node node)
        {
            base.HandleEvent(node);

            if (node.Name == name)
            {
                node.OnRecieveMessage(this.Client, msg);
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
