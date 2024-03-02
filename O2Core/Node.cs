using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    public class NodeTimer
    {
        private long elapsed;

        public NodeTimer(long elapsedMs)
        {
            this.elapsed = elapsedMs;
        }

        public long ElapsedMs => elapsed;
    }

    public class Node
    {
        private readonly string name;
        private readonly List<NodeTimer> timers = new List<NodeTimer>();

        public Node(string name)
        {
            this.name = name;
        }

        public string Name => name;

        public ReadOnlyCollection<NodeTimer> Timers { get { return timers.AsReadOnly(); } }

        public void AddTimer(NodeTimer timer)
        {
            this.timers.Add(timer);
        }

        public void RemoveTimer(NodeTimer timer)
        {
            this.timers.Remove(timer);
        }

        public virtual void OnTimer(NodeTimer timer)
        {
            // Do nothing
        }

        public virtual void OnRecieveMessage(Client client, Message msg)
        {
            // Do nothing
        }

        public virtual void OnClientDisconnected(Client client)
        {
            // Do nothing
        }

        protected void SendNack(Client client, int errorCode, string msg, string messageName)
        {
            Message response = new Message(this.name, messageName);
            response.WriteString("NACK");
            response.WriteInt(errorCode);
            response.WriteString(msg);
            client.Send(response);
        }

        protected void SendAck(Client client, string messageName)
        {
            Message response = new Message(this.name, messageName);
            response.WriteString("ACK");
            client.Send(response);
        }
    }

    internal class TimeNode : Node
    {
        public TimeNode() : base("TIME")
        {
        }

        public override void OnRecieveMessage(Client client, Message msg)
        {
            base.OnRecieveMessage(client, msg);

            DateTime dateTime = DateTime.Now;

            Message response = new Message(this.Name, "");
            response.WriteString(dateTime.ToString("HH:mm:ss ddMMyyyy"));

            client.Send(response);
        }
    }
}
