using System.Runtime.InteropServices;

namespace Oxygen
{
    internal class Plugin
    {
        public string? Name { get; set; }
        public bool ManualStart { get; set; }
        public string? Type { get; set; }
        public string? Filter { get; set; }
        public string? Package { get; set; }
        public Time? Time { get; set; }

        private readonly List<PluginAction> actions = new List<PluginAction>();
        private readonly List<string> artefacts = new List<string>();
        private readonly List<PluginResult> results = new List<PluginResult>();
        private readonly Dictionary<long, PluginNotificationStream> streams = new Dictionary<long, PluginNotificationStream>();

        public IList<PluginAction> Actions => actions;
        public IList<string> Artefacts => artefacts;
        public IList<PluginResult> Results => results;

        public void StartNotificationStream(long userId, Request request)
        {
            if (!streams.ContainsKey(userId))
            {
                streams.Add(userId, new PluginNotificationStream(request));
            }
        }

        public void CloseNotificationStream(long userId)
        {
            if (streams.TryGetValue(userId, out PluginNotificationStream? stream) && stream != null)
            {
                stream.StreamEnded();
                stream.StreamData();
            }

            streams.Remove(userId);
        }

        public void AddAction(PluginAction action)
        {
            actions.Add(action);
        }

        public void AddArtefact(string artefact)
        {
            this.artefacts.Add(artefact);
        }

        public void AddResult(PluginResult result)
        {
            this.results.Add(result);

            foreach (var stream in streams)
            {
                stream.Value.TaskCompleted(result);
            }

            StreamData();
        }

        public void OnStart(long userId)
        {
            if (streams.TryGetValue(userId, out PluginNotificationStream? stream) && stream != null)
            {
                stream.TaskStarted();
                stream.StreamData();
            }
        }

        private void StreamData()
        {
            foreach (var stream in streams)
            {
                stream.Value.StreamData();
            }
        }

        public void Schedule(Schedule schedule)
        {
            if (Time.HasValue)
            {
                DateTime now = DateTime.Now;
                DateTime date = new DateTime(now.Year, now.Month, now.Day, Time.Value.Hour, Time.Value.Minute, Time.Value.Second);

                // If we are already past the calculated time, schedule it for the next day.
                if (now > date)
                {
                    date = date.AddDays(1);
                }

                schedule.StartAt(this, date, TimeSpan.FromMinutes(20));
            }
        }
    }

    internal struct Time
    {
        public int Hour { get; private set; }
        public int Minute { get; private set; }
        public int Second { get; private set; }

        public Time(int hour, int minute, int second)
        {
            this.Hour = hour;
            this.Minute = minute;
            this.Second = second;
        }

        public Time(string time)
        {
            int sep1 = time.IndexOf(":");
            int sep2 = time.IndexOf(":", sep1 + 1);

            this.Hour = int.Parse(time.Substring(0, sep1));
            this.Minute = int.Parse(time.Substring(sep1 + 1, sep2 - sep1 - 1));
            this.Second = int.Parse(time.Substring(sep2 + 1));
        }
    }

    internal class PluginResult
    {
        public string StartedBy { get; private set; }
        public string Artefact { get; private set; }

        public PluginResult(string startedBy, string artefact)
        {
            this.StartedBy = startedBy;
            this.Artefact = artefact;
        }
    }

    internal class PluginAction
    {
        public string? Run { get; set; }
        public string? Args { get; set; }
    }
}
