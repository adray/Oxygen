#include "DownloadStream.h"
#include "Message.h"
#include "ClientConnection.h"
#include "Subscriber.h"
#include <memory>

using namespace Oxygen;

constexpr int STREAM_METADATA = 0;
constexpr int STREAM_TRANSFER = 1;
constexpr int STREAM_DATA = 2;
constexpr int STREAM_PROTOCOL_ERROR = 3;
constexpr int STREAM_OPEN = 4;
constexpr int STREAM_STATUS = 244;
constexpr int STREAM_END = 255;

constexpr int STATUS_OK = 0;
constexpr int STATUS_ERROR = 1;

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

void DownloadStream::OnDataDownloaded(Message& msg)
{
    const int numBytes = msg.ReadInt32();
    std::vector<unsigned char> data(numBytes);
    msg.ReadBytes(numBytes, data.data());

    _received += numBytes;
    _downloadStream.write((char*)data.data(), numBytes);
}

void DownloadStream::OnStatus(Message& msg)
{
    const int status = msg.ReadInt32();
    if (status == STATUS_ERROR)
    {
        _isDownloading = false;
        _downloadStream.close();

        _downloadCallback();
    }
}

void DownloadStream::OnTransfer(Message& msg)
{
    std::string file = msg.ReadString();
    int size = msg.ReadInt32();
    int bufferSize = msg.ReadInt32();

    _downloadStream = std::ofstream(_dir + "/" + file, std::ios::binary);
    if (!_downloadStream.good())
    {
        // close stream?
    }
}

void DownloadStream::OnStreamEnded(Message& msg)
{
    _downloadStream.close();
    _isDownloading = false;
    _downloadCallback();
}

void DownloadStream::OnProtocolError(Message& msg)
{
    const std::string error = msg.ReadString();
    _isDownloading = false;
    _isError = true;
    _downloadCallback();
}

void DownloadStream::Download(const std::string& dir, const std::string& name, const std::function<void()>& callback)
{
    if (!_isDownloading)
    {
        _downloadCallback = callback;
        _isDownloading = true;
        _dir = dir;
        _isError = false;

        Message msg(_node, _msgName);
        msg.WriteInt32(STREAM_OPEN);
        msg.WriteString(name);

        BuildStreamStart(msg);
        //msg.WriteString(""); // checksum

        std::shared_ptr<Subscriber> sub = std::make_shared<Subscriber>(msg);
        _conn->AddSubscriber(sub);
        sub->Signal([this, sub2 = sub](Oxygen::Message& msg)
            {
                const int type = msg.ReadInt32();
                switch (type)
                {
                case STREAM_STATUS:
                    OnStatus(msg);
                    break;
                case STREAM_TRANSFER:
                    OnTransfer(msg);
                    break;
                case STREAM_PROTOCOL_ERROR:
                    OnProtocolError(msg);
                    _conn->RemoveSubscriber(sub2);
                    break;
                case STREAM_DATA:
                    OnDataDownloaded(msg);
                    break;
                case STREAM_END:
                    OnStreamEnded(msg);
                    _conn->RemoveSubscriber(sub2);
                    break;
                }
            });
    }
}
