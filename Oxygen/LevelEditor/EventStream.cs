using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class EventStream
    {
        private readonly Dictionary<Client, LevelEventStream> eventStreams = new Dictionary<Client, LevelEventStream>();
        private readonly object streamLock = new object();

        public bool AddStream(Client client, LevelEventStream stream)
        {
            bool success = false;
            lock (this.streamLock)
            {
                if (!eventStreams.ContainsKey(client))
                {
                    this.eventStreams.Add(client, stream);
                    success = true;
                }
            }
            return success;
        }

        public void RemoveStream(Client client)
        {
            lock (this.streamLock)
            {
                this.eventStreams.Remove(client);
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

        public void MoveUserCursor(int id, int objectId)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.eventStreams)
                {
                    stream.Value.MoveUserCursor(id, objectId);
                }
            }
        }
    }
}
