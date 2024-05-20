using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class PluginEngine : Node
    {
        private Dictionary<string, Plugin> plugins = new Dictionary<string, Plugin>();
        private Schedule schedule = new Schedule();
        private NodeTimer timer = new NodeTimer(1000 * 30);

        public PluginEngine() : base("PLUGIN_SVR")
        {
            this.AddTimer(timer);

            this.Load();
        }

        public override void OnTimer(NodeTimer timer)
        {
            base.OnTimer(timer);

            if (timer == this.timer)
            {
                schedule.CheckSchedule();
            }
        }

        public override void OnTrigger(string condition)
        {
            base.OnTrigger(condition);

            schedule.Trigger(condition);
        }

        public override void OnClientDisconnected(Client client)
        {
            base.OnClientDisconnected(client);

            foreach (var plugin in plugins.Values)
            {
                plugin.CloseNotificationStream(client.ID);
            }
        }

        public override void OnRecieveMessage(Request request)
        {
            base.OnRecieveMessage(request);

            if (!Authorizer.IsAuthorized(request))
            {
                return;
            }

            string? user = (string?)request.Client.GetProperty("USER_NAME");
            if (user == null)
            {
                SendNack(request, 100, "Error: user not found.", request.Message.MessageName);
                return;
            }

            Message msg = request.Message;

            if (request.Message.MessageName == "LIST_PLUGINS")
            {
                Message response = Response.Ack(this, msg.MessageName);
                response.WriteInt(plugins.Count);
                foreach (var plugin in plugins)
                {
                    if (!string.IsNullOrEmpty(plugin.Value.Name))
                    {
                        response.WriteString(plugin.Value.Name);
                    }
                }
                request.Send(response);
            }
            else if (request.Message.MessageName == "SCHEDULE_LIST")
            {
                Message response = Response.Ack(this, msg.MessageName);
                schedule.PrintSchedule(response);
                request.Send(response);
            }
            else if (request.Message.MessageName == "SCHEDULE_PLUGIN")
            {
                string name = msg.ReadString();
                bool startNow = msg.ReadInt() == 1;

                if (plugins.TryGetValue(name, out var plugin))
                {
                    if (plugin.ManualStart)
                    {
                        if (startNow)
                        {
                            schedule.StartNow(plugin, user, request.Client.ID);
                            SendAck(request, msg.MessageName);
                        }
                        else
                        {
                            // TODO: get a date time to start at.
                        }
                    }
                    else
                    {
                        SendNack(request, 100, "This plugin cannot be started manually.", msg.MessageName);
                    }
                }
                else
                {
                    SendNack(request, 100, "No such plugin.", msg.MessageName);
                }
            }
            else if (request.Message.MessageName == "NOTIFICATION_STREAM")
            {
                string name = msg.ReadString();

                if (plugins.TryGetValue(name, out var plugin))
                {
                    plugin.StartNotificationStream(request.Client.ID, request);
                    SendAck(request, msg.MessageName);
                }
                else
                {
                    SendNack(request, 100, "No such plugin.", msg.MessageName);
                }
            }
            else if (request.Message.MessageName == "CLOSE_NOTIFICATION_STREAM")
            {
                string name = msg.ReadString();

                if (plugins.TryGetValue(name, out var plugin))
                {
                    plugin.CloseNotificationStream(request.Client.ID);
                    SendAck(request, msg.MessageName);
                }
                else
                {
                    SendNack(request, 100, "No such plugin.", msg.MessageName);
                }
            }
            else
            {
                SendNack(request, 100, "No such message.", msg.MessageName);
            }
        }

        private void Load()
        {
            string pluginFile = "Plugins\\Plugins.json";
            if (Directory.Exists("Plugins") && File.Exists(pluginFile))
            {
                byte[] pluginData = File.ReadAllBytes(pluginFile);
                PluginReader reader = new PluginReader();

                try
                {
                    reader.Load(pluginData);
                }
                catch (JsonException e)
                {
                    Logger.Instance.Log(e.Message);
                }

                foreach (var plugin in reader.Plugins)
                {
                    if (!string.IsNullOrEmpty(plugin.Name))
                    {
                        plugin.Schedule(schedule);
                        this.plugins.Add(plugin.Name, plugin);
                    }
                    else
                    {
                        Logger.Instance.Log("Attempted to plugin with no name defined.");
                    }
                }
            }
            else
            {
                Logger.Instance.Log("No plugins loaded. No plugin directory.");
            }
        }
    }
}
