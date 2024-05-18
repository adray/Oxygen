using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Schedule
    {
        private class ScheduleItem
        {
            public DateTime StartTime { get; private set; }
            public DateTime EndTime { get; private set; }
            public Plugin Plugin { get; private set; }
            public string StartedBy { get; private set; }
            public bool StartedManually { get; private set; }
            public long UserId { get; private set; }

            public ScheduleItem(Plugin plugin, DateTime startTime, TimeSpan timeout, string startedBy, bool startedManually, long userId)
            {
                this.Plugin = plugin;
                this.StartTime = startTime;
                this.EndTime = startTime + timeout;
                this.StartedBy = startedBy;
                this.StartedManually = startedManually;
                this.UserId = userId;
            }
        }

        private List<ScheduleItem> scheduleItems = new List<ScheduleItem>();
        private List<PluginTask> running = new List<PluginTask>();
        private List<KeyValuePair<Plugin, Trigger>> triggers = new List<KeyValuePair<Plugin, Trigger>>();

        public void StartAt(Plugin plugin, DateTime startTime, TimeSpan timeout)
        {
            this.scheduleItems.Add(new ScheduleItem(plugin, startTime, timeout, "Schedule", false, -1));
        }

        public void StartNow(Plugin plugin, string startedBy, long userId)
        {
            this.scheduleItems.Add(new ScheduleItem(plugin, DateTime.Now.AddSeconds(10), TimeSpan.FromMinutes(20), startedBy, true, userId));
        }

        public void TriggerOn(Plugin plugin, Trigger trigger)
        {
            this.triggers.Add(new KeyValuePair<Plugin, Trigger>(plugin, trigger));
        }

        private void Run(object? o)
        {
            var item = o as PluginTask;
            if (item != null)
            {
                item.Start();
            }
        }

        private static void InjectAssets(Plugin plugin, PluginTask task)
        {
            if (plugin.Type == "Bake")
            {
                Directory.CreateDirectory(Path.Combine(task.Workspace, "Assets"));

                IList<string> assets = Archiver.GetAssets();
                
                foreach (string asset in assets)
                {
                    bool copy = true;
                    if (!string.IsNullOrEmpty(plugin.Filter))
                    {
                        // TODO: check file matches the filter!
                    }

                    if (copy)
                    {
                        File.Copy(Path.Combine("Assets", asset), Path.Combine(task.Workspace, "Assets", asset));
                    }
                }
            }
        }

        public void CheckSchedule()
        {
            DateTime now = DateTime.Now;
            foreach (ScheduleItem item in scheduleItems)
            {
                if (now >= item.StartTime)
                {
                    var task = new PluginTask(item.Plugin, item.StartedManually, item.StartedBy, item.UserId);
                    StartTask(item, task);
                    scheduleItems.Remove(item);
                    break;
                }
            }

            foreach (var task in running)
            {
                if (!task.Running)
                {
                    running.Remove(task);

                    task.Plugin.AddResult(new PluginResult(task.StartedBy, task.Artefacts));

                    if (!task.StartedManually)
                    {
                        // Schedule for next time.
                        task.Plugin.Schedule(this);
                    }
                    break;
                }
            }
        }

        private void StartTask(ScheduleItem item, PluginTask task)
        {
            if (task.StartedManually)
            {
                task.Plugin.OnStart(item.UserId);
            }

            running.Add(task);

            InjectAssets(item.Plugin, task);

            ThreadPool.QueueUserWorkItem(Run, task);
        }

        public void Trigger(string condition)
        {
            foreach (var trigger in triggers)
            {
                bool isTriggered = false;
                foreach (var item in trigger.Value.Conditions)
                {
                    if (item == condition)
                    {
                        isTriggered = true;
                        break;
                    }
                }

                if (isTriggered)
                {
                    StartAt(trigger.Key, DateTime.Now, TimeSpan.FromMinutes(20));
                }
            }
        }
    }
}
