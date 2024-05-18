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
            // TODO: schedule to start a time
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
