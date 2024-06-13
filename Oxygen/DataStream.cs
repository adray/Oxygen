using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace Oxygen
{
    internal class DataStreamBase
    {
        private readonly string nodeName;
        private readonly string downloadMessageName;
        private readonly string destDir;
        private const int BUFFER_SIZE = 2048;

        private const int STREAM_METADATA = 0;
        private const int STREAM_TRANSFER = 1;
        private const int STREAM_DATA = 2;
        private const int STREAM_PROTOCOL_ERROR = 3;
        private const int STREAM_OPEN = 4;
        private const int STREAM_STATUS = 244;
        private const int STREAM_END = 255;

        private const int STATUS_OK = 0;
        private const int STATUS_ERROR = 1;

        private readonly object downloadStreamLock = new object();
        private readonly Dictionary<Client, DownloadStream> downloadStreams = new Dictionary<Client, DownloadStream>();
        private readonly Dictionary<Client, UploadStream> uploadStreams = new Dictionary<Client, UploadStream>();

        public static void StatusError(Request request, string errorMessage)
        {
            Message msg = request.Message;
            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteInt(STREAM_STATUS);
            response.WriteInt(STATUS_ERROR);
            response.WriteString(errorMessage);
            request.Send(response);
        }

        private static void ProtocolError(Request request)
        {
            Message msg = request.Message;
            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteInt(STREAM_PROTOCOL_ERROR);
            request.Send(response);
        }

        protected DataStreamBase(string nodeName, string downloadMessageName, string destDir)
        {
            this.nodeName = nodeName;
            this.downloadMessageName = downloadMessageName;
            this.destDir = destDir;
        }

        private class UploadStream
        {
            private readonly DataStreamBase dataStream;
            private FileStream? stream;
            private int size;
            private int bufferSize;
            private int bytesWritten;
            private bool open = true;
            private string? filename;

            public bool Open => this.open;

            public UploadStream(DataStreamBase dataStream)
            {
                this.dataStream = dataStream;
            }

            public void ProcessRequest(Request request)
            {
                var msg = request.Message;
                int type = request.Message.ReadInt();
                switch (type)
                {
                    case STREAM_DATA:
                        OnData(msg, request.Client);
                        break;
                    case STREAM_TRANSFER:
                        OnTransfer(msg, request.Client);
                        break;
                    case STREAM_END:
                        OnEnd(msg);
                        break;
                    case STREAM_PROTOCOL_ERROR:
                        OnError(msg);
                        break;
                }
            }

            private void OnError(Message msg)
            {
                string error = msg.ReadString();

                Logger.Instance.Log("Error uploading file '{0}'", error);

                stream?.Dispose();
                stream = null;

                string path = dataStream.destDir + @"\" + filename;
                if (Path.Exists(path))
                {
                    File.Delete(path);
                }
            }

            private void OnEnd(Message msg)
            {
                this.open = false;
            }

            private void OnData(Message msg, Client client)
            {
                byte[] data = msg.ReadByteArray();

                try
                {
                    stream?.Write(data);
                    bytesWritten += data.Length;
                }
                catch (IOException ex)
                {

                }

                if (bytesWritten == size)
                {
                    stream?.Dispose();
                    stream = null;

                    this.dataStream.OnUploadCompleted(this.filename, client);
                }
            }

            private void OnTransfer(Message msg, Client client)
            {
                this.filename = msg.ReadString();
                this.size = msg.ReadInt();
                this.bufferSize = msg.ReadInt();

                this.dataStream.OnUploadTransferStarted(this.filename, client);

                try
                {
                    stream = File.OpenWrite(dataStream.destDir + @"\" + filename);
                }
                catch (DirectoryNotFoundException ex)
                {

                }
            }
        }

        private class DownloadStream
        {
            private readonly Request request;
            private readonly DataStreamBase stream;
            private bool connected = true;

            public DownloadStream(Request request, DataStreamBase stream)
            {
                this.request = request;
                this.stream = stream;
            }

            public void SendMetadata(string filename, object? userData)
            {
                Message msg = new Message(stream.nodeName, stream.downloadMessageName);
                msg.WriteInt(STREAM_METADATA);

                this.stream.SendDownloadMetadata(filename, userData, msg);

                this.request.Send(msg);
            }

            private void StartTransfer(string filename, long size)
            {
                Message msg = new Message(stream.nodeName, stream.downloadMessageName);
                msg.WriteInt(STREAM_TRANSFER);
                msg.WriteString(filename);
                msg.WriteInt((int)size);
                msg.WriteInt(BUFFER_SIZE);

                this.request.Send(msg);
            }

            public void SendError(string error)
            {
                Message msg = new Message(stream.nodeName, stream.downloadMessageName);
                msg.WriteInt(STREAM_PROTOCOL_ERROR);
                msg.WriteString(error);

                this.request.Send(msg);
            }

            public void StreamFile(string path)
            {
                string filename = Path.GetFileName(path);

                try
                {
                    using (FileStream file = File.OpenRead(path))
                    {
                        long totalBytes = file.Length;

                        StartTransfer(filename, totalBytes);

                        byte[] buffer = new byte[BUFFER_SIZE];
                        int count;
                        while (file.Position < totalBytes && this.connected)
                        {
                            count = file.Read(buffer, 0, BUFFER_SIZE);

                            Message msg = new Message(stream.nodeName, stream.downloadMessageName);
                            msg.WriteInt(STREAM_DATA);
                            msg.WriteInt(count);
                            msg.WriteBytes(buffer, count);
                            this.request.Send(msg);
                        }
                    }
                }
                catch (IOException)
                {
                    SendError("Stream failed to download.");
                }
            }

            public void CloseStream()
            {
                Message msg = new Message(stream.nodeName, stream.downloadMessageName);
                msg.WriteInt(STREAM_END);

                this.request.Send(msg);
            }

            public void ClientDisconnected()
            {
                this.connected = false;
            }
        }

        public void CloseStreams(Client client)
        {
            lock (downloadStreamLock)
            {
                if (downloadStreams.TryGetValue(client, out DownloadStream? stream))
                {
                    stream.ClientDisconnected();
                    downloadStreams.Remove(client);
                }
            }

            uploadStreams.Remove(client);
        }

        private void StatusOK(Request request)
        {
            Message status = new Message(this.nodeName, this.downloadMessageName);
            status.WriteInt(STREAM_STATUS);
            status.WriteInt(STATUS_OK);
            request.Send(status);
        }

        private void DownloadNotRequired(Request request, string filename, object? metaData)
        {
            // Stream status.
            StatusOK(request);

            // Send metadata.
            Message metaDataMsg = new Message(this.nodeName, this.downloadMessageName);
            metaDataMsg.WriteInt(STREAM_METADATA);
            SendDownloadMetadata(filename, metaData, metaDataMsg);
            request.Send(metaDataMsg);

            // Close the download stream.
            Message response = new Message(this.nodeName, this.downloadMessageName);
            response.WriteInt(STREAM_END);
            request.Send(response);
        }

        private void StartDownloadStream(Request request, string filename, object? metaData)
        {
            if (IsDownloadCached(filename, metaData))
            {
                DownloadNotRequired(request, filename, metaData);
                return;
            }

            DownloadStream stream = new DownloadStream(request, this);
            bool cancel = false;
            lock (downloadStreamLock)
            {
                cancel = downloadStreams.ContainsKey(request.Client);
                if (!cancel)
                {
                    downloadStreams.Add(request.Client, stream);
                }
            }

            if (cancel)
            {
                StatusError(request, "Download stream already open.");
            }
            else
            {
                // Send status.
                StatusOK(request);

                // Write the metadata on the main thread, thus the callback is on the main thread
                stream.SendMetadata(filename, metaData);

                // Now, start the streaming from another thread.
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    stream.StreamFile(destDir + @"\" + filename);
                    stream.CloseStream();

                    lock (downloadStreamLock)
                    {
                        downloadStreams.Remove(request.Client);
                    }
                });
            }
        }

        public void ProcessDownloadStreamMessage(Request request)
        {
            var msg = request.Message;
            int type = msg.ReadInt();
            if (type == STREAM_OPEN)
            {
                string name = msg.ReadString();
                object? data = ReadDownloadMetaData(msg);
                StartDownloadStream(request, name, data);
            }
            else
            {
                ProtocolError(request);
            }
        }

        public void ProcessUploadStreamMessage(Request request)
        {
            if (this.uploadStreams.TryGetValue(request.Client, out UploadStream? stream) && stream != null)
            {
                stream.ProcessRequest(request);
            }
            else
            {
                int type = request.Message.ReadInt();
                if (type == STREAM_OPEN)
                {
                    this.uploadStreams.Add(request.Client, new UploadStream(this));

                    Message msg = new Message(request.Message.NodeName, request.Message.MessageName);
                    msg.WriteInt(STREAM_STATUS);
                    msg.WriteInt(STATUS_OK);
                    request.Send(msg);
                }
                else
                {
                    Message msg = new Message(request.Message.NodeName, request.Message.MessageName);
                    msg.WriteInt(STREAM_PROTOCOL_ERROR);
                    request.Send(msg);
                }
            }
        }

        protected virtual object? ReadDownloadMetaData(Message msg)
        {
            return null;
        }

        protected virtual void SendDownloadMetadata(string filename, object? userData, Message msg)
        {
            // Do nothing
        }

        protected virtual void OnUploadTransferStarted(string filename, Client client)
        {
            // Do nothing
        }

        protected virtual void OnUploadCompleted(string filename, Client client)
        {
            // Do nothing.
        }

        protected virtual bool IsDownloadCached(string filename, object? userData)
        {
            return false;
        }
    }

    internal class AssetDataStream : DataStreamBase
    {
        private readonly Cache cache;

        public AssetDataStream(Cache cache) : base("ASSET_SVR", "ASSET_DOWNLOAD_STREAM", "Assets")
        {
            this.cache = cache;
        }

        protected override object? ReadDownloadMetaData(Message msg)
        {
            int hasChecksum = msg.ReadInt();

            string? checksum = null;
            if (hasChecksum == 1)
            {
                checksum = msg.ReadString();
            }

            return checksum;
        }

        protected override bool IsDownloadCached(string filename, object? userData)
        {
            string? userChecksum = (string?)userData;

            bool clientHasLatest = false;
            if (userChecksum != null)
            {
                string? checksum = this.cache.GetChecksum(@"Assets\" + filename);
                if (string.IsNullOrEmpty(checksum))
                {
                    checksum = this.cache.CacheItem(@"Assets\" + filename);
                    this.cache.SaveCache();
                }

                if (userChecksum != null && checksum == userChecksum)
                {
                    clientHasLatest = true;
                }
            }

            return clientHasLatest;
        }

        protected override void SendDownloadMetadata(string filename, object? userData, Message msg)
        {
            base.SendDownloadMetadata(filename, userData, msg);

            string? userChecksum = this.cache.GetChecksum(@"Assets\" + filename);

            // Send the checksum
            if (userChecksum != null)
            {
                msg.WriteString(filename); // filename
                msg.WriteString(userChecksum ?? string.Empty); // checksum
            }
        }

        protected override void OnUploadTransferStarted(string filename, Client client)
        {
            base.OnUploadTransferStarted(filename, client);

            Archiver.BackupAsset(@"Assets\" + filename);
        }

        protected override void OnUploadCompleted(string filename, Client client)
        {
            base.OnUploadCompleted(filename, client);

            string? user = client.GetProperty("USER_NAME") as string;

            if (user != null)
            {
                this.cache.CacheItem(@"Assets\" + filename);
                this.cache.SaveCache();
                Archiver.ArchiveAsset(@"Assets\" + filename, user);
                Audit.Instance.Log("Asset {0} upload finished by user {1}.", filename, user);
            }
        }
    }

    internal class ArtefactDataStream : DataStreamBase
    {
        public ArtefactDataStream() : base("BUILD_SVR", "ARTEFACT_DOWNLOAD_STREAM", "Artefacts")
        {
        }
    }

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
