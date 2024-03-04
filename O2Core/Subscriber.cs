namespace Oxygen
{
    public class Subscriber
    {
        public string NodeName { get; private set; }
        public string MessageName { get; private set; }

        public event EventHandler<ClientException>? Error;
        internal event EventHandler<Message>? MessageReady;

        public Subscriber(string nodeName, string messageName)
        {
            this.NodeName = nodeName;
            this.MessageName = messageName;
        }

        public virtual void OnMessageRecieved(Message msg)
        {

        }

        protected void OnError(Message msg)
        {
            int errorCode = msg.ReadInt();
            string errorMsg = msg.ReadString();

            this.OnError(new ClientException(errorCode, errorMsg));
        }

        protected void OnError(ClientException e)
        {
            this.Error?.Invoke(this, e);
        }

        protected void SendMessage(Message msg)
        {
            if (this.MessageReady != null)
            {
                this.MessageReady(this, msg);
            }
        }
    }
}
