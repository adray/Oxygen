using System.Xml.Linq;

namespace Oxygen
{
    internal class UploadStream
    {
        private const int STREAM_METADATA = 0;
        private const int STREAM_TRANSFER = 1;
        private const int STREAM_DATA = 2;
        private const int STREAM_ERROR = 3;
        private const int STREAM_OPEN = 4;
        private const int STREAM_STATUS = 244;
        private const int STREAM_END = 255;

        private const int STATUS_OK = 0;
        private const int STATUS_ERROR = 1;

        private const int BUFFER_SIZE = 2048;

        private FileStream? stream;
        private byte[] buffer = new byte[BUFFER_SIZE];

        public long Length => stream?.Length ?? 0;
        public long Pos => stream?.Position ?? 0;

        public string? Error { get; private set; }
        public bool Closed { get; private set; }

        public void SendStreamStart(Message msg)
        {
            msg.WriteInt(STREAM_OPEN);
        }

        public void SendMetadata(Message msg)
        {
            msg.WriteInt(STREAM_METADATA);
            WriteMetaData(msg);
        }

        public void SendTransfer(Message msg, string filename)
        {
            msg.WriteInt(STREAM_TRANSFER);

            try
            {
                stream = File.OpenRead(filename);
            }
            catch (IOException ex)
            {

            }

            if (stream != null)
            {
                msg.WriteString(Path.GetFileName(filename));
                msg.WriteInt((int)stream.Length);
                msg.WriteInt(BUFFER_SIZE);
            }
        }

        public void SendData(Message msg)
        {
            msg.WriteInt(STREAM_DATA);

            if (stream != null)
            {
                int numBytes = stream.Read(buffer, 0, BUFFER_SIZE);

                msg.WriteInt(numBytes);
                msg.WriteBytes(buffer, numBytes);
            }
        }

        public void SendError(Message msg, string error)
        {
            msg.WriteInt(STREAM_ERROR);
            msg.WriteString(error);
        }

        public void SendClose(Message msg)
        {
            msg.WriteInt(STREAM_END);
        }

        public void OnServerResponse(Message msg)
        {
            int type = msg.ReadInt();
            switch (type)
            {
                case STREAM_ERROR:
                    this.Error = msg.ReadString();
                    break;
                case STREAM_END:
                    this.Closed = true;
                    break;
                case STREAM_STATUS:
                    int status = msg.ReadInt();
                    if (status == STATUS_ERROR)
                    {
                        this.Error = msg.ReadString();
                    }
                    break;
            }
        }

        public void CloseFile()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

        protected virtual void WriteMetaData(Message msg)
        {
            // do nothing
        }
    }
}
