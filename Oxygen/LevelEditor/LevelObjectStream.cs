namespace Oxygen
{
    /// <summary>
    /// Per client level object stream.
    /// </summary>
    internal class LevelObjectStream
    {
        private readonly Client client;
        private Queue<Message> newObjects = new Queue<Message>();
        private Queue<Message> removeObjects = new Queue<Message>();
        private Queue<Message> updates = new Queue<Message>();
        private Dictionary<int, byte[]> state = new Dictionary<int, byte[]>();

        private const int NEW_OBJECT = 0;
        private const int UPDATE_OBJECT = 1;
        private const int DELETE_OBJECT = 2;

        public LevelObjectStream(Client client)
        {
            this.client = client;
        }

        public void AddObject(LevelObject obj)
        {
            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(NEW_OBJECT);
            obj.Serialize(msg);

            newObjects.Enqueue(msg);

            byte[] bytes = msg.GetData();
            state.Add(obj.ID, bytes);
        }

        public void RemoveObject(int id)
        {
            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(DELETE_OBJECT);
            msg.WriteInt(id);
            removeObjects.Enqueue(msg);
        }

        public void UpdateObject(LevelObject obj)
        {
            //
            // Generates a delta compressed message (within another message)
            // NOTE: if multiple updates occur quickly, this will not compact the updates.
            // And will send an update for each change to the object.

            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(NEW_OBJECT);
            obj.Serialize(msg);

            byte[] bytes = state[obj.ID];
            byte[] newBytes = msg.GetData();
            byte[] compressBytes = DeltaCompress.Compress(bytes, newBytes);

            Message msg2 = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg2.WriteInt(UPDATE_OBJECT);
            msg2.WriteInt(obj.ID);
            msg2.WriteInt(obj.Version);
            msg2.WriteInt(compressBytes.Length);
            msg2.WriteBytes(compressBytes);

            updates.Enqueue(msg2);

            state[obj.ID] = newBytes;
        }

        public void StreamData()
        {
            while (newObjects.Count > 0)
            {
                client.Send(newObjects.Dequeue());
            }

            while (removeObjects.Count > 0)
            {
                client.Send(removeObjects.Dequeue());
            }
            
            while (updates.Count > 0)
            {
                client.Send(updates.Dequeue());
            }
        }
    }
}
