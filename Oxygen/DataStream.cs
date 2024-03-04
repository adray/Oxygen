using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Oxygen
{
    internal class DataStream
    {
        private class UploadStream
        {
            private string name;
            private int totalBytes;
            private int pos;
            private BinaryWriter writer;

            public UploadStream(int totalBytes, string name)
            {
                this.name = name;
                this.totalBytes = totalBytes;
                this.writer = new BinaryWriter(File.OpenWrite(name));
            }

            public bool AddBytes(byte[] bytes)
            {
                if (bytes.Length + pos <= totalBytes)
                {
                    writer.Write(bytes);
                    pos += bytes.Length;

                    if (pos == totalBytes)
                    {
                        this.writer.Close();
                    }
                }
                else
                {
                    this.writer.Close();
                    return false;
                }

                return true;
            }

            public bool Completed => pos == totalBytes;

            public string Name => name;
        }

        private class DownloadStream
        {
            private long totalBytes;
            private BinaryReader? reader;
            private bool completed;

            public DownloadStream(string name)
            {
                try
                {
                    FileStream file = File.OpenRead(name);
                    totalBytes = file.Length;
                    this.reader = new BinaryReader(file);
                }
                catch (IOException)
                {
                    this.completed = true;
                }
            }

            public long TotalBytes => totalBytes;

            public byte[]? ReadBytes(int numBytes)
            {
                byte[]? bytes = null;
                if (reader != null)
                {
                    bytes = reader.ReadBytes(numBytes);
                    if (reader.BaseStream.Position == reader.BaseStream.Length)
                    {
                        this.completed = true;
                        reader.Close();
                    }
                }

                return bytes;
            }

            public bool Completed => this.completed;
        }

        private readonly Dictionary<Client, UploadStream> uploadStreams = new Dictionary<Client, UploadStream>();
        private readonly Dictionary<Client, DownloadStream> downloadStreams = new Dictionary<Client, DownloadStream>();

        public long AddDownloadStream(Client client, string streamFile)
        {
            if (downloadStreams.ContainsKey(client))
            {
                return -1;
            }

            DownloadStream stream = new DownloadStream(streamFile);
            downloadStreams.Add(client, stream);
            return stream.TotalBytes;
        }

        public byte[]? DownloadBytes(Client client, int numBytes)
        {
            if (downloadStreams.TryGetValue(client, out DownloadStream? stream))
            {
                byte[]? bytes = stream.ReadBytes(numBytes);
                if (stream.Completed)
                {
                    CloseDownloadStream(client);
                }
                return bytes;
            }

            return null;
        }

        public void AddUploadStream(Client client, string streamFile, int totalBytes)
        {
            uploadStreams.Add(client, new UploadStream(totalBytes, streamFile));
        }

        public string GetUploadStreamName(Client client)
        {
            if (uploadStreams.TryGetValue(client, out UploadStream? stream))
            {
                return stream.Name;
            }

            return string.Empty;
        }

        public bool UploadBytes(Client client, byte[] bytes)
        {
            if (uploadStreams.TryGetValue(client, out UploadStream? stream))
            {
                if (stream.AddBytes(bytes))
                {
                    if (stream.Completed)
                    {
                        CloseUploadStream(client);
                        return true;
                    }
                }
                else
                {
                    CloseUploadStream(client);
                }
            }

            return false;
        }

        public void CloseUploadStream(Client client)
        {
            uploadStreams.Remove(client);
        }

        public void CloseDownloadStream(Client client)
        {
            downloadStreams.Remove(client);
        }
    }
}
