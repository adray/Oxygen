namespace Oxygen
{
    internal class ObjectStream
    {
        private readonly Dictionary<Request, LevelObjectStream> streams = new Dictionary<Request, LevelObjectStream>();
        private readonly object streamLock = new object();

        public bool Add(Request request, LevelObjectStream stream)
        {
            bool success = false;
            lock (this.streamLock)
            {
                if (!this.streams.ContainsKey(request))
                {
                    this.streams.Add(request, stream);
                    success = true;
                }
            }
            return success;
        }

        public void Remove(Request request)
        {
            lock (this.streamLock)
            {
                this.streams.Remove(request);
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
