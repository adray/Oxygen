#pragma once
#include <string>
#include <functional>
#include <fstream>

namespace Oxygen
{
    class ClientConnection;

    class DownloadStream
    {
    public:
        DownloadStream(ClientConnection* conn, const std::string& node, const std::string& msgName);
        void Download(const std::string& dir, const std::string& name, const std::function<void()>& callback);

    private:
        void DownloadPart();
        
        ClientConnection* _conn;

        std::string _node;
        std::string _msgName;

        bool _isDownloading;
        std::function<void()> _downloadCallback;
        std::ofstream _downloadStream;

        int _filesize;
        int _received;
    };
}
