using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oxygen
{
    internal class AssetDownloadStream : IDownloadStream
    {
        private BinaryWriter? fileWriter = null;
        private int recieved = 0;
        private readonly string name;
        private readonly Cache cache;
        private int numBytes = 0;
        private string? checksum;

        public AssetDownloadStream(string name, Cache cache)
        {
            this.name = name;
            this.cache = cache;
        }

        public void Download(Message response)
        {
            string ack = response.ReadString();

            if (ack == "ACK")
            {
                checksum = response.ReadString();
                if (checksum != cache.GetChecksum(name))
                {
                    File.Delete(name);
                    fileWriter = new BinaryWriter(File.OpenWrite(name));

                    numBytes = response.ReadInt();

                    byte[] data = response.ReadByteArray();

                    recieved += data.Length;
                    fileWriter.Write(data);
                }
            }
            else
            {
                int errorCode = response.ReadInt();
                string errorMessage = response.ReadString();

                throw new ClientException(errorCode, errorMessage);
            }
        }

        public bool Completed => recieved == numBytes;

        public void Close()
        {
            fileWriter?.Close();
            fileWriter?.Dispose();

            if (checksum != null)
            {
                cache.CacheItem(name, checksum);
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

        public Message SendDownloadPart()
        {
            return new Message("ASSET_SVR", "DOWNLOAD_ASSET_PART");
        }

        public Message SendDownload()
        {
            Message msg = new Message("ASSET_SVR", "DOWNLOAD_ASSET");
            msg.WriteString(name);
            string? checksum = cache.GetChecksum(name);
            msg.WriteInt(checksum != null ? 1 : 0);
            if (checksum != null)
            {
                msg.WriteString(checksum);
            }
            return msg;
        }
    }
}
