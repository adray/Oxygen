using System.Text;

namespace Oxygen
{
    public class Message : IDisposable
    {
        private BinaryReader? reader;
        private BinaryWriter? writer;
        private MemoryStream? stream;

        public string NodeName { get; private set; }
        public string MessageName { get; private set; }

        internal int Id { get; set; }

        public Message(byte[] data)
        {
            this.stream = new MemoryStream(data);
            this.reader = new BinaryReader(stream);
            this.NodeName = this.ReadString();
            this.MessageName = this.ReadString();
        }

        public Message(string nodeName, string messageName)
        {
            this.NodeName = nodeName;
            this.MessageName = messageName;

            this.stream = new MemoryStream();
            this.writer = new BinaryWriter(this.stream);
            this.WriteString(nodeName);
            this.WriteString(messageName);
        }

        public long Length
        {
            get
            {
                if (this.stream == null)
                {
                    throw new InvalidOperationException();
                }

                return this.stream.Length;
            }
        }

        public long Position
        {
            get
            {
                if (this.stream == null)
                {
                    throw new InvalidOperationException();
                }

                return this.stream.Position;
            }
        }

        public byte[] GetData()
        {
            if (this.stream == null)
            {
                throw new InvalidOperationException();
            }

            return this.stream.ToArray();
        }

        public void WriteString(string value)
        {
            if (this.writer == null)
            {
                throw new InvalidOperationException();
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            this.writer.Write(bytes.Length);
            this.writer.Write(bytes);
        }

        public void WriteInt(int value)
        {
            if (this.writer == null)
            {
                throw new InvalidOperationException();
            }

            this.writer.Write(value);
        }

        public void WriteInt64(long value)
        {
            if (this.writer == null)
            {
                throw new InvalidOperationException();
            }

            this.writer.Write(value);
        }

        public void WriteDouble(double value)
        {
            if (this.writer == null)
            {
                throw new InvalidOperationException();
            }

            this.writer.Write(value);
        }

        public void WriteBytes(byte[] bytes)
        {
            if (this.writer == null)
            {
                throw new InvalidOperationException();
            }

            this.writer.Write(bytes);
        }

        public void WriteBytes(byte[] bytes, int size)
        {
            if (this.writer == null)
            {
                throw new InvalidOperationException();
            }

            this.writer.Write(bytes, 0, size);
        }

        private static Exception CreateMalformedException(Exception ex)
        {
            return new Exception("Malformed message.", ex);
        }

        public string ReadString()
        {
            if (this.reader == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                int numBytes = reader.ReadInt32();
                byte[] bytes = reader.ReadBytes(numBytes);

                return Encoding.UTF8.GetString(bytes);
            }
            catch (IOException ex)
            {
                throw CreateMalformedException(ex);
            }
        }

        public int ReadInt()
        {
            if (this.reader == null)
            {
                throw new InvalidOperationException();
            }
            try
            {
                return reader.ReadInt32();
            }
            catch (IOException ex)
            {
                throw CreateMalformedException(ex);
            }
        }

        public double ReadDouble()
        {
            if (this.reader == null)
            {
                throw new InvalidOperationException();
            }
            try
            {
                return reader.ReadDouble();
            }
            catch (IOException ex)
            {
                throw CreateMalformedException(ex);
            }
        }

        public byte[] ReadByteArray()
        {
            if (this.reader == null)
            {
                throw new InvalidOperationException();
            }
            try
            {
                int numBytes = reader.ReadInt32();
                return reader.ReadBytes(numBytes);
            }
            catch (IOException ex)
            {
                throw CreateMalformedException(ex);
            }
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
