namespace Oxygen
{
    internal class ArtefactDownloadStream : IDownloadStream
    {
        private BinaryWriter? fileWriter = null;
        private readonly string name;
        private int numBytes = 0;
        private int recieved = 0;

        public bool Completed => recieved == numBytes;

        public ArtefactDownloadStream(string name)
        {
            this.name = name;
        }

        public void Download(Message response)
        {
            string ack = response.ReadString();

            if (ack == "ACK")
            {
                File.Delete(name);
                fileWriter = new BinaryWriter(File.OpenWrite(name));

                numBytes = response.ReadInt();

                byte[] data = response.ReadByteArray();

                recieved += data.Length;
                fileWriter.Write(data);
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMessage = response.ReadString();

                throw new ClientException(errorCode, errorMessage);
            }
        }

        public void DownloadPart(Message response)
        {
            string ack = response.ReadString();
            if (ack == "ACK")
            {
                byte[] data = response.ReadByteArray();

                recieved += data.Length;
                fileWriter?.Write(data);
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMessage = response.ReadString();

                throw new ClientException(errorCode, errorMessage);
            }
        }

        public Message SendDownload()
        {
            Message msg = new Message("BUILD_SVR", "DOWNLOAD_ARTEFACT");
            msg.WriteString(name);
            return msg;
        }

        public Message SendDownloadPart()
        {
            return new Message("BUILD_SVR", "DOWNLOAD_ARTEFACT_PART");
        }

        public void Close()
        {
            fileWriter?.Close();
            fileWriter?.Dispose();
        }
    }
}
