namespace Oxygen
{
    public class Message : IDisposable
    {
        private BinaryReader? reader;
        private BinaryWriter? writer;
        private MemoryStream? stream;

        public string NodeName { get; private set; }
        public string MessageName { get; private set; }

        public Message(byte[] data)
        {
            this.reader = new BinaryReader(new MemoryStream(data));
            this.NodeName = this.ReadString();
            this.MessageName = this.ReadString();
        }

        public Message(string nodeName, string messageName)
        {
            this.stream = new MemoryStream();
            this.writer = new BinaryWriter(this.stream);
            this.writer.Write(nodeName);
            this.writer.Write(messageName);
        }

        public byte[] GetData()
        {
            return this.stream.ToArray();
        }

        public void WriteString(string value)
        {
            this.writer.Write(value);
        }

        public void WriteInt(int value)
        {
            this.writer.Write(value);
        }

        public void WriteDouble(double value)
        {
            this.writer.Write(value);
        }

        public void WriteBytes(byte[] bytes)
        {
            this.writer.Write(bytes);
        }

        public string ReadString()
        {
            return reader.ReadString();
        }

        public int ReadInt()
        {
            return reader.ReadInt32();
        }

        public double ReadDouble()
        {
            return reader.ReadDouble();
        }

        public byte[] ReadByteArray()
        {
            int numBytes = reader.ReadInt32();
            return reader.ReadBytes(numBytes);
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }

            if (this.writer != null)
            {
                this.writer.Dispose();
                this.writer = null;
            }

            GC.SuppressFinalize(this);
        }

        ~Message()
        {
            this.Dispose();
        }
    }
}
