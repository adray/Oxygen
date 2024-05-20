#include "DownloadStream.h"
#include "Message.h"
#include "ClientConnection.h"
#include "Subscriber.h"
#include <memory>

using namespace Oxygen;

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

DownloadStream::DownloadStream(ClientConnection* conn, const std::string& node, const std::string& msgName)
    :
    _conn(conn),
    _node(node),
    _msgName(msgName),
    _isDownloading(false),
    _filesize(0),
    _received(0)
{
}

void DownloadStream::DownloadPart()
{
    if (_received < _filesize)
    {
        Message msg(_node, _msgName + "_PART");
        std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
        _conn->AddSubscriber(sub);
        sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
            {
                if (msg.ReadString() == "ACK")
                {
                    const int numBytes = msg.ReadInt32();
                    std::vector<unsigned char> data(numBytes);
                    msg.ReadBytes(numBytes, data.data());

                    _received += numBytes;
                    _downloadStream.write((char*)data.data(), numBytes);
                    DownloadPart();
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

void DownloadStream::Download(const std::string& dir, const std::string& name, const std::function<void()>& callback)
{
    if (!_isDownloading)
    {
        _downloadCallback = callback;
        _downloadStream = std::ofstream(dir + "/" + name, std::ios::binary);
        if (_downloadStream.good())
        {
            _isDownloading = true;

            Message msg(_node, _msgName);
            msg.WriteString(name);
            //msg.WriteInt32(0); // if contains checksum - ONLY for assets!
            std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
            _conn->AddSubscriber(sub);
            sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
                {
                    if (msg.ReadString() == "ACK")
                    {
                        //std::string checksum = msg.ReadString();
                        _filesize = msg.ReadInt32();
                        const int numBytes = msg.ReadInt32();
                        std::vector<unsigned char> data(numBytes);
                        msg.ReadBytes(numBytes, data.data());

                        _received += numBytes;
                        _downloadStream.write((char*)data.data(), numBytes);

                        DownloadPart();
                    }
                    else
                    {
                        _isDownloading = false;
                    }

                    _conn->RemoveSubscriber(sub2);
                });
        }
    }
}
