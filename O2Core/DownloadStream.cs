namespace Oxygen
{
    internal class DownloadStream
    {
        private const int STREAM_METADATA = 0;
        private const int STREAM_TRANSFER = 1;
        private const int STREAM_DATA = 2;
        private const int STREAM_PROTOCOL_ERROR = 3;
        private const int STREAM_OPEN = 4;
        private const int STREAM_STATUS = 254;
        private const int STREAM_END = 255;

        private const int STATUS_ERROR = 1;

        private string? file;
        private long size;
        private int bufferSize;
        private FileStream? stream = null;
        private int bytesTransferred;
        private string? errorMsg;
        private bool endOfStream;

        public long Size => size;
        public long BytesTransferred => bytesTransferred;
        public string? ErrorMessage => errorMsg;

        public void SendDownloadStream(Message msg, string file)
        {
            msg.WriteInt(STREAM_OPEN);
            msg.WriteString(file);
            OnSendDownloadStream(msg, file);
        }

        public void Download(Message msg)
        {
            int type = msg.ReadInt();
            switch (type)
            {
                case STREAM_STATUS:
                    OnStatus(msg);
                    break;
                case STREAM_METADATA:
                    OnMetaDataReceived(msg);
                    break;
                case STREAM_TRANSFER:
                    OnTransfer(msg);
                    break;
                case STREAM_DATA:
                    OnData(msg);
                    break;
                case STREAM_PROTOCOL_ERROR:
                    OnError(msg);
                    break;
                case STREAM_END:
                    this.endOfStream = true;
                    break;
            }
        }

        public bool EndOfStream => this.endOfStream;

        private void OnStatus(Message msg)
        {
            int status = msg.ReadInt();
            if (status == STATUS_ERROR)
            {
                errorMsg = msg.ReadString();
                this.endOfStream = true;
            }
        }

        private void OnError(Message msg)
        {
            errorMsg = msg.ReadString();
            this.endOfStream = true;
        }

        private void OnData(Message msg)
        {
            byte[] data = msg.ReadByteArray();

            try
            {
                this.stream?.Write(data, 0, data.Length);
            }
            catch (IOException ex)
            {

            }

            bytesTransferred += data.Length;
            if (bytesTransferred == this.size)
            {
                this.stream?.Close();
                
                if (file != null)
                {
                    this.OnDownloadComplete(this.file);
                }
            }
        }

        private void OnTransfer(Message msg)
        {
            this.file = msg.ReadString();
            this.size = msg.ReadInt();
            this.bufferSize = msg.ReadInt();

            try
            {
                this.stream = File.OpenWrite(this.file);
            }
            catch (DirectoryNotFoundException e)
            {

            }
        }

        protected virtual void OnSendDownloadStream(Message msg, string file)
        {
            // do nothing
        }

        protected virtual void OnDownloadComplete(string filename)
        {
            // do nothing
        }

        protected virtual void OnMetaDataReceived(Message msg)
        {
            // do nothing
        }
    }
}
