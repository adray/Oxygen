#include "AssetService.h"
#include "Subscriber.h"
#include "ClientConnection.h"
#include <memory>
#include <codecvt>

constexpr int chunkSize = 1024;

#ifdef _WINDOWS
#define WIN32_MEAN_AND_LEAN
#define NOMINMAX
#include <Windows.h>

static int GetFileSize(const std::wstring& filepath)
{
    HANDLE handle = CreateFileW(filepath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    LARGE_INTEGER size;
    if (handle && GetFileSizeEx(handle, &size))
    {
        return (int)size.QuadPart;
    }

    return 0;
}

#endif

using namespace Oxygen;

AssetService_DownloadStream::AssetService_DownloadStream(ClientConnection* conn)
    : DownloadStream(conn, "ASSET_SVR", "ASSET_DOWNLOAD_STREAM")
{
}

void AssetService_DownloadStream::BuildStreamStart(Message& msg)
{
    msg.WriteInt32(0);  // no checksum
}

AssetService::AssetService(ClientConnection* conn, const std::string& assetDir)
    : _conn(conn), _assetDir(assetDir), _isDownloading(false), _isUploading(false)
{
}

void AssetService::GetAssetList(const std::function<void(std::vector<std::string>& assets)>& callback)
{
    _assetListCallback = callback;

    Message msg("ASSET_SVR", "ASSET_LIST");
    std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
    _conn->AddSubscriber(sub);
    sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
        {
            if (msg.ReadString() == "ACK")
            {
                const int numAssets = msg.ReadInt32();
                std::vector<std::string> assets;
                for (int i = 0; i < numAssets; i++)
                {
                    assets.push_back(msg.ReadString());
                }

                _assetListCallback(assets);
            }
            else
            {
                const int code = msg.ReadInt32();
            }

            _conn->RemoveSubscriber(sub2);
        });
}

void AssetService::UploadAsset(const std::string& asset, const std::function<void()>& callback)
{
    if (!_isUploading)
    {
        _uploadStream = std::shared_ptr<UploadStream>(new UploadStream(_conn, _assetDir, "ASSET_SVR", "ASSET_UPLOAD_STREAM"));
        _uploadStream->Upload(asset, [this, callback2 = callback]() {
            _isUploading = false;
            callback2();
            });
    }
}

void AssetService::DownloadAsset(const std::string& asset, const std::function<void()>& callback)
{
    if (!_isDownloading)
    {
        _downloadStream = std::shared_ptr<AssetService_DownloadStream>(new AssetService_DownloadStream(_conn));
        _downloadStream->Download(_assetDir, asset, [this, callback2=callback]() {
            _isDownloading = false;
            callback2();
            });
    }
}
