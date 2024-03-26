using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class LevelEventStream
    {
        private readonly Client client;
        private Queue<Message> events = new Queue<Message>();

        private const int USER_CONNECTED = 0;
        private const int USER_DISCONNECTED = 1;
        private const int MOVE_USER_CURSOR = 2;

        public LevelEventStream(Client client)
        {
            this.client = client;
        }

        public void UserConnected(long id, string username)
        {
            Message message = new Message("LEVEL_SVR", "EVENT_STREAM");
            message.WriteInt(USER_CONNECTED);
            message.WriteInt64(id);
            message.WriteString(username);
            events.Enqueue(message);
        }

        public void UserDisconnected(long id, string username)
        {
            Message message = new Message("LEVEL_SVR", "EVENT_STREAM");
            message.WriteInt(USER_DISCONNECTED);
            message.WriteInt64(id);
            message.WriteString(username);
            events.Enqueue(message);
        }

        public void MoveUserCursor(long id, int objectID)
        {
            Message message = new Message("LEVEL_SVR", "EVENT_STREAM");
            message.WriteInt(MOVE_USER_CURSOR);
            message.WriteInt64(id);
            message.WriteInt(objectID);
            events.Enqueue(message);
        }

        public void StreamData()
        {
            while (events.Count > 0)
            {
                client.Send(events.Dequeue());
            }
        }
    }
}
