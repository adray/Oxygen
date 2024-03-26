namespace Oxygen
{
    internal class ObjectStream
    {
        private readonly Dictionary<Client, LevelObjectStream> streams = new Dictionary<Client, LevelObjectStream>();
        private readonly object streamLock = new object();

        public bool Add(Client client, LevelObjectStream stream)
        {
            bool success = false;
            lock (this.streamLock)
            {
                if (!this.streams.ContainsKey(client))
                {
                    this.streams.Add(client, stream);
                    success = true;
                }
            }
            return success;
        }

        public void Remove(Client client)
        {
            lock (this.streamLock)
            {
                this.streams.Remove(client);
            }
        }

        public void StreamData()
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.streams)
                {
                    stream.Value.StreamData();
                }
            }
        }

        public void AddObject(LevelObject obj)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.streams)
                {
                    stream.Value.AddObject(obj);
                }
            }
        }

        public void UpdateObject(LevelObject obj)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.streams)
                {
                    stream.Value.UpdateObject(obj);
                }
            }
        }

        public void RemoveObject(int id)
        {
            lock (this.streamLock)
            {
                foreach (var stream in this.streams)
                {
                    stream.Value.RemoveObject(id);
                }
            }
        }
    }
}
