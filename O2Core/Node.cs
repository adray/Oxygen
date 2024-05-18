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

        public virtual void OnRecieveMessage(Request request)
        {
            // Do nothing
        }

        public virtual void OnClientDisconnected(Client client)
        {
            // Do nothing
        }

        public virtual void AddMetric(Metric metric)
        {
            // Do nothing
        }

        public virtual void OnTrigger(string condition)
        {
            // Do nothing
        }

        protected void SendNack(Request request, int errorCode, string msg, string messageName)
        {
            Message response = Response.Nack(this, errorCode, msg, messageName);
            request.Send(response);
        }

        protected void SendAck(Request request, string messageName)
        {
            Message response = Response.Ack(this, messageName);
            request.Send(response);
        }
    }

    internal class TimeNode : Node
    {
        public TimeNode() : base("TIME")
        {
        }

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            DateTime dateTime = DateTime.Now;

            Message response = new Message(this.Name, request.Message.MessageName);
            response.WriteString(dateTime.ToString("HH:mm:ss ddMMyyyy"));

            request.Send(response);
        }
    }
}
