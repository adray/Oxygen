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
            Level.LoadLevels();
        }

        public override void OnClientDisconnected(Client client)
        {
            base.OnClientDisconnected(client);

            Level? level = client.GetProperty("LEVEL") as Level;
            if (level != null)
            {
                CloseLevel(client, level);
            }
        }

        private void CloseLevel(Client client, Level level)
        {
            level.RemoveClient(client);
            client.RemoveProperty("LEVEL");
            if (!level.Running)
            {
                levels.Remove(level.Name);
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
                Level? existingLevel = client.GetProperty("LEVEL") as Level;
                if (existingLevel != null)
                {
                    existingLevel.RemoveClient(client);
                    client.RemoveProperty("LEVEL");
                }

                string levelName = msg.ReadString();
                if (levels.TryGetValue(levelName, out Level? level) && level != null)
                {
                    client.SetProperty("LEVEL", level);
                    level.AddClient(client);
                    SendAck(client, messageName);
                }
                else
                {
                    level = Level.LoadFromFile(levelName);

                    if (level != null)
                    {
                        levels.Add(levelName, level);
                        level.AddClient(client);
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
                    CloseLevel(client, level);
                    SendAck(client, messageName);
                }
                else
                {
                    SendNack(client, 200, "No level open to close.", messageName);
                }
            }
            else if (messageName == "OBJECT_STREAM")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.AddStream(client, Level.Stream.Object);
                }
            }
            else if (messageName == "NEW_LEVEL")
            {
                string levelName = msg.ReadString();
                if (!levels.ContainsKey(levelName))
                {
                    levels.Add(levelName, Level.NewLevel(levelName));
                    SendAck(client, messageName);
                }
                else
                {
                    SendNack(client, 200, "Level already exists.", messageName);
                }
            }
            else if (messageName == "LIST_LEVELS")
            {
                Message response = new Message(this.Name, messageName);
                response.WriteString("ACK");
                response.WriteInt(Level.Levels.Count);
                foreach (var level in Level.Levels)
                {
                    response.WriteString(level);
                }
                client.Send(response);
            }
            else if (messageName == "ADD_OBJECT")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.AddObject(client, msg);
                }
                else
                {
                    SendNack(client, 100, "No level opened.", msg.MessageName);
                }
            }
            else if (messageName == "UPDATE_OBJECT")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.UpdateObject(client, msg);
                }
                else
                {
                    SendNack(client, 100, "No level opened.", msg.MessageName);
                }
            }
            else if (messageName == "EVENT_STREAM")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.AddStream(client, Level.Stream.Event);
                }
            }
            else if (messageName == "UPDATE_CURSOR")
            {
                Level? level = client.GetProperty("LEVEL") as Level;
                if (level != null)
                {
                    level.MoveCursor(client, msg);
                    SendAck(client, msg.MessageName);
                }
                else
                {
                    SendNack(client, 100, "No level opened.", msg.MessageName);
                }
            }
            else
            {
                SendNack(client, 100, "Request failed.", messageName);
            }
        }
    }
}
