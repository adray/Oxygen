using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class LevelServer : Node
    {
        private readonly Dictionary<string, Level> levels = new Dictionary<string, Level>();

        public LevelServer()
            : base("LEVEL_SVR")
        {
        }

        public override void OnClientDisconnected(Client client)
        {
            base.OnClientDisconnected(client);
        }

        public override void OnRecieveMessage(Client client, Message msg)
        {
            base.OnRecieveMessage(client, msg);

            if (!Authorizer.IsAuthorized(client, msg))
            {
                return;
            }

            string? user = client.GetProperty("USER_NAME") as string;
            if (user == null)
            {
                SendNack(client, 0, "Internal error", msg.MessageName);
                return;
            }

            string messageName = msg.MessageName;
            if (messageName == "LOAD_LEVEL")
            {
                string levelName = msg.ReadString();
                if (levels.TryGetValue(levelName, out Level? level) && level != null)
                {
                    client.SetProperty("LEVEL", level);
                    SendAck(client, messageName);
                }
                else
                {
                    level = LoadLevelFromDisk(levelName);

                    if (level != null)
                    {
                        client.SetProperty("LEVEL", level);
                        SendAck(client, messageName);
                    }
                    else
                    {
                        SendNack(client, 200, "No such level.", messageName);
                    }
                }
            }
            else if (messageName == "ADD_OBJECT")
            {
                LevelObject obj = new LevelObject();

                string objectType = msg.ReadString();

                double posX = msg.ReadDouble();
                double posY = msg.ReadDouble();
                double posZ = msg.ReadDouble();
                double scaleX = msg.ReadDouble();
                double scaleY = msg.ReadDouble();
                double scaleZ = msg.ReadDouble();
                double rotationX = msg.ReadDouble();
                double rotationY = msg.ReadDouble();
                double rotationZ = msg.ReadDouble();

                Transform transform = new Transform();
                transform.SetPos(posX, posY, posZ);
                transform.SetScale(scaleX, scaleY, scaleZ);
                transform.SetRotation(rotationX, rotationY, rotationZ);

                obj.Transform = transform;

                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.AddObject(obj);
                }
            }
            else
            {
                SendNack(client, 100, "Request failed.", messageName);
            }
        }

        private Level? LoadLevelFromDisk(string levelName)
        {


            return null;
        }
    }
}
