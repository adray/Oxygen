using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class EventStream
    {
        private readonly Dictionary<Request, LevelEventStream> eventStreams = new Dictionary<Request, LevelEventStream>();
        private readonly object streamLock = new object();

        public bool AddStream(Request request, LevelEventStream stream)
        {
            bool success = false;
            lock (this.streamLock)
            {
                if (!eventStreams.ContainsKey(request))
                {
                    this.eventStreams.Add(request, stream);
                    success = true;
                }
            }
            return success;
        }

        public void RemoveStream(Request request)
        {
            lock (this.streamLock)
            {
                this.eventStreams.Remove(request);
            }
        }

        public void StreamData()
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.eventStreams)
                {
                    stream.Value.StreamData();
                }
            }
        }

        public void UserConnected(long id, string user)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.eventStreams)
                {
                    stream.Value.UserConnected(id, user);
                }
            }
        }

        public void UserDisconnected(long id, string user)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.eventStreams)
                {
                    stream.Value.UserDisconnected(id, user);
                }
            }
        }

        public void MoveUserCursor(long id, int objectId, int subID)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.eventStreams)
                {
                    stream.Value.MoveUserCursor(id, objectId, subID);
                }
            }
        }
    }
}
