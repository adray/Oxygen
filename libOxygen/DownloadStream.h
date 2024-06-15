#pragma once
#include <string>
#include <functional>
#include <fstream>

namespace Oxygen
{
    class ClientConnection;
    class Message;

    class DownloadStream
    {
    public:
        DownloadStream(ClientConnection* conn, const std::string& node, const std::string& msgName);
        void Download(const std::string& dir, const std::string& name, const std::function<void()>& callback);
        
        inline bool IsError() { return _isError; }

        virtual ~DownloadStream() {};

    protected:
        virtual void BuildStreamStart(Message& msg) {}

    private:
        void OnDataDownloaded(Message& msg);
        void OnStatus(Message& msg);
        void OnTransfer(Message& msg);
        void OnStreamEnded(Message& msg);
        void OnProtocolError(Message& msg);

        ClientConnection* _conn;
        bool _isError;

        std::string _node;
        std::string _msgName;

        bool _isDownloading;
        std::function<void()> _downloadCallback;
        std::ofstream _downloadStream;

        int _filesize;
        int _received;
        std::string _dir;
    };
}
