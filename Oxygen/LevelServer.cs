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

            Level? level = client.GetProperty("LEVEL") as Level;
            if (level != null)
            {
                level.RemoveClient(client);
                client.RemoveProperty("LEVEL");
            }
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
            else if (messageName == "CLOSE_LEVEL")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.RemoveClient(client);
                    client.RemoveProperty("LEVEL");
                }
            }
            else if (messageName == "OBJECT_STREAM")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.AddClient(client);
                }
            }
            else if (messageName == "NEW_LEVEL")
            {
                string levelName = msg.ReadString();
                if (!levels.ContainsKey(levelName))
                {
                    levels.Add(levelName, new Level());
                    SendAck(client, messageName);
                }
                else
                {
                    SendNack(client, 200, "Level already exists.", messageName);
                }
            }
            else if (messageName == "ADD_OBJECT")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.AddObject(msg);
                }
            }
            else if (messageName == "UPDATE_OBJECT")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.UpdateObject(msg);
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
