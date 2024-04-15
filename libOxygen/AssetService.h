#pragma once
#include <string>
#include <vector>
#include <functional>
#include <fstream>

namespace Oxygen
{
    class ClientConnection;

    class AssetService
    {
    public:
        AssetService(ClientConnection* conn, const std::string& assetDir);

        void GetAssetList(const std::function<void(std::vector<std::string>& assets)>& callback);
        void DownloadAsset(const std::string& asset, const std::function<void()>& callback);
        void UploadAsset(const std::string& asset, const std::function<void()>& callback);

    private:
        void DownloadAssetPart();
        void UploadAssertPart();

        ClientConnection* _conn;
        const std::string _assetDir;
        std::function<void(std::vector<std::string>& assets)> _assetListCallback;
        std::function<void()> _downloadCallback;
        std::ofstream _downloadStream;
        std::function<void()> _uploadCallback;
        std::ifstream _uploadStream;
        int _filesize;
        int _recieved;
        int _sent;
        bool _isDownloading;
        bool _isUploading;
    };
}
