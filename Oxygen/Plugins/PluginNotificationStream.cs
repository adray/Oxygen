using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class PluginNotificationStream
    {
        private readonly Request request;
        private Queue<Message> events = new Queue<Message>();

        private const int TASK_STARTED = 0;
        private const int TASK_COMPLETED = 1;
        private const int STREAM_ENDED = 255;

        public PluginNotificationStream(Request request)
        {
            this.request = request;
        }

        public void TaskStarted()
        {
            Message message = new Message("PLUGIN_SVR", "NOTIFICATION_STREAM");
            message.WriteInt(TASK_STARTED);
            events.Enqueue(message);
        }

        public void TaskCompleted(PluginResult result)
        {
            Message message = new Message("PLUGIN_SVR", "NOTIFICATION_STREAM");
            message.WriteInt(TASK_COMPLETED);
            message.WriteString(result.Artefact);
            events.Enqueue(message);
        }

        public void StreamEnded()
        {
            Message message = new Message("PLUGIN_SVR", "NOTIFICATION_STREAM");
            message.WriteInt(STREAM_ENDED);
            events.Enqueue(message);
        }

        public void StreamData()
        {
            while (events.Count > 0)
            {
                request.Send(events.Dequeue());
            }
        }
    }
}
