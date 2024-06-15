#pragma once
#include <string>
#include <vector>
#include <functional>
#include <fstream>
#include "DownloadStream.h"
#include "UploadStream.h"

namespace Oxygen
{
    class ClientConnection;

    class AssetService_DownloadStream : public DownloadStream
    {
    public:
        AssetService_DownloadStream(ClientConnection* conn);

    protected:
        virtual void BuildStreamStart(Message& msg);
    };

    class AssetService
    {
    public:
        AssetService(ClientConnection* conn, const std::string& assetDir);

        void GetAssetList(const std::function<void(std::vector<std::string>& assets)>& callback);
        void DownloadAsset(const std::string& asset, const std::function<void()>& callback);
        void UploadAsset(const std::string& asset, const std::function<void()>& callback);

        inline bool IsUploadError() { return _uploadStream->IsError(); }
        inline bool IsDownloadError() { return _downloadStream->IsError(); }

    private:

        ClientConnection* _conn;
        const std::string _assetDir;
        std::function<void(std::vector<std::string>& assets)> _assetListCallback;
        bool _isDownloading;
        bool _isUploading;
        std::shared_ptr<AssetService_DownloadStream> _downloadStream;
        std::shared_ptr<UploadStream> _uploadStream;
    };
}
