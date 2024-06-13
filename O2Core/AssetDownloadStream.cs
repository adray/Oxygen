using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oxygen
{
    internal class AssetDownloadStream : DownloadStream
    {
        private readonly Cache cache;

        public string Checksum { get; private set; } = string.Empty;

        public AssetDownloadStream(Cache cache)
        {
            this.cache = cache;
        }

        protected override void OnSendDownloadStream(Message msg, string file)
        {
            base.OnSendDownloadStream(msg, file);

            string checksum = cache.GetChecksum(file) ?? string.Empty;
            msg.WriteString(checksum);
        }

        protected override void OnMetaDataReceived(Message msg)
        {
            base.OnMetaDataReceived(msg);

            string file = msg.ReadString();
            this.Checksum = msg.ReadString();

            if (Checksum != cache.GetChecksum(file))
            {
                // Delete the file if it exists, there is a newer file to download.
                File.Delete(file);
            }
        }

        protected override void OnDownloadComplete(string file)
        {
            base.OnDownloadComplete(file);

            this.cache.CacheItem(file, this.Checksum);
        }
    }
}
