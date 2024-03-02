using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2Client
{
    public class APIKeySubscriber : Subscriber
    {
        public event EventHandler LoggedIn;

        public APIKeySubscriber()
            : base("LOGIN_SVR", "LOGIN_API_KEY")
        {
        }

        public override void OnMessageRecieved(Message msg)
        {
            base.OnMessageRecieved(msg);

            string ack = msg.ReadString();
            if (ack == "ACK")
            {
                this.OnLogin();
            }
            else
            {
                this.OnError(msg);
            }
        }

        private void OnLogin()
        {
            if (LoggedIn != null)
            {
                LoggedIn(this, EventArgs.Empty);
            }
        }

        public void Send(string api_key)
        {
            Message msg = new Message(this.NodeName, this.MessageName);
            msg.WriteString(api_key);
            this.SendMessage(msg);
        }
    }
}
