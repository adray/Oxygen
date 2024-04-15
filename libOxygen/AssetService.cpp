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

int GetFileSize(const std::wstring& filepath)
{
    HANDLE handle = CreateFileW(filepath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    LARGE_INTEGER size;
    if (handle && GetFileSizeEx(handle, &size))
    {
        return size.QuadPart;
    }

    return 0;
}

#endif

using namespace Oxygen;

AssetService::AssetService(ClientConnection* conn, const std::string& assetDir)
    : _conn(conn), _assetDir(assetDir), _recieved(0), _filesize(0), _sent(0), _isDownloading(false), _isUploading(false)
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

void AssetService::DownloadAssetPart()
{
    if (_recieved < _filesize)
    {
        Message msg("ASSET_SVR", "DOWNLOAD_ASSET_PART");
        std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
        _conn->AddSubscriber(sub);
        sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
            {
                if (msg.ReadString() == "ACK")
                {
                    const int numBytes = msg.ReadInt32();
                    std::vector<unsigned char> data(numBytes);
                    msg.ReadBytes(numBytes, data.data());

                    _recieved += numBytes;
                    _downloadStream.write((char*)data.data(), numBytes);
                    DownloadAssetPart();
                }

                _conn->RemoveSubscriber(sub2);
            });
    }
    else
    {
        _downloadStream.close();
        _isDownloading = false;
        _downloadCallback();
    }
}

void AssetService::DownloadAsset(const std::string& asset, const std::function<void()>& callback)
{
    if (!_isDownloading)
    {
        _downloadCallback = callback;
        _downloadStream = std::ofstream(_assetDir + "/" + asset, std::ios::binary);
        if (_downloadStream.good())
        {
            _isDownloading = true;

            Message msg("ASSET_SVR", "DOWNLOAD_ASSET");
            msg.WriteString(asset);
            msg.WriteInt32(0);
            std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
            _conn->AddSubscriber(sub);
            sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
                {
                    if (msg.ReadString() == "ACK")
                    {
                        std::string checksum = msg.ReadString();
                        _filesize = msg.ReadInt32();
                        const int numBytes = msg.ReadInt32();
                        std::vector<unsigned char> data(numBytes);
                        msg.ReadBytes(numBytes, data.data());

                        _recieved += numBytes;
                        _downloadStream.write((char*)data.data(), numBytes);

                        DownloadAssetPart();
                    }

                    _conn->RemoveSubscriber(sub2);
                });
        }
    }
}

void AssetService::UploadAssertPart()
{
    if (!_uploadStream.eof())
    {
        Message msg("ASSET_SVR", "UPLOAD_ASSET_PART");
        unsigned char buffer[chunkSize];
        _uploadStream.read((char*)buffer, chunkSize);

        int numBytes = std::min(chunkSize, _sent);
        msg.WriteBytes(numBytes, buffer);
        _sent -= numBytes;

        std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
        _conn->AddSubscriber(sub);
        sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
            {
                if (msg.ReadString() == "ACK")
                {
                    UploadAssertPart();
                }

                _conn->RemoveSubscriber(sub2);
            });
    }
    else
    {
        _isUploading = false;
        _uploadStream.close();
        _uploadCallback();
    }
}

void AssetService::UploadAsset(const std::string& asset, const std::function<void()>& callback)
{
    if (!_isUploading)
    {
        const std::string path = _assetDir + "/" + asset;

        _uploadCallback = callback;
        _uploadStream = std::ifstream(path, std::ios::binary);
        if (_uploadStream.good())
        {
            _isUploading = true;

            std::wstring_convert<std::codecvt_utf8_utf16<wchar_t> > converter;
            const int size = GetFileSize(converter.from_bytes(path));

            if (size > 0)
            {
                _sent = size;

                Message msg("ASSET_SVR", "UPLOAD_ASSET");
                msg.WriteString(asset);
                msg.WriteInt32(size);

                unsigned char buffer[chunkSize];
                _uploadStream.read((char*)buffer, chunkSize);

                const int numBytes = std::min(chunkSize, size);
                msg.WriteBytes(numBytes, buffer);

                _sent -= numBytes;

                std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
                _conn->AddSubscriber(sub);
                sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
                    {
                        if (msg.ReadString() == "ACK")
                        {
                            UploadAssertPart();
                        }

                        _conn->RemoveSubscriber(sub2);
                    });
            }
        }
    }
}
