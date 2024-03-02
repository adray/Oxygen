using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2Client
{
    public class AssetListSubscriber : Subscriber
    {
        private List<string> assets = new List<string>();

        public event EventHandler Loaded;

        public AssetListSubscriber()
            : base("ASSET_SVR", "ASSET_LIST")
        {
        }

        public override void OnMessageRecieved(Message msg)
        {
            base.OnMessageRecieved(msg);

            string ack = msg.ReadString();
            if (ack == "ACK")
            {
                int numAssets = msg.ReadInt();
                for (int i = 0; i < numAssets; i++)
                {
                    assets.Add(msg.ReadString());
                }
                this.OnLoaded();
            }
            else
            {
                this.OnError(msg);
            }
        }

        public void Send()
        {
            this.assets.Clear();

            Message msg = new Message(this.NodeName, this.MessageName);
            this.SendMessage(msg);
        }

        private void OnLoaded()
        {
            this.Loaded?.Invoke(this, EventArgs.Empty);
        }

        public IList<string> Assets => this.assets;
    }
}
