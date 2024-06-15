#pragma once
#include <fstream>
#include <thread>
#include "Subscriber.h"
#include "ClientConnection.h"

namespace Oxygen
{
    class UploadStream
    {
    public:
        UploadStream(ClientConnection* conn, const std::string& dir, const std::string& nodeName, const std::string& messageName);

        void Upload(const std::string& asset, const std::function<void()>& callback);

        inline bool IsError() { return _error; }

    private:
        void OnStatus(Message& msg);
        void UploadThread();

        std::thread _thread;
        ClientConnection* _conn;

        std::string _nodeName;
        std::string _messageName;

        std::string _assetName;
        std::string _dir;
        std::function<void(std::vector<std::string>& assets)> _assetListCallback;
        std::function<void()> _uploadCallback;
        std::ifstream _uploadStream;
        std::shared_ptr<Subscriber> _sub;
        int _sent;
        bool _isUploading;
        bool _error;
    };
}

