namespace Oxygen
{
    internal interface IDownloadStream
    {
        void Download(Message response);
        void DownloadPart(Message response);
        Message SendDownloadPart();
        Message SendDownload();
        bool Completed { get; }
        void Close();
    }
}
