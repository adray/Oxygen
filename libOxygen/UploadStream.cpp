#include "UploadStream.h"
#include <algorithm>
#include <codecvt>

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

using namespace Oxygen;

UploadStream::UploadStream(ClientConnection* conn, const std::string& dir, const std::string& nodeName, const std::string& messageName)
    :
    _conn(conn),
    _isUploading(false),
    _sent(0),
    _dir(dir),
    _nodeName(nodeName),
    _messageName(messageName),
    _error(false)
{
}

void UploadStream::UploadThread()
{
    const int size = _sent;
    const int chunkSize = 1024;

    unsigned char buffer[chunkSize];

    Message transfer(_nodeName, _messageName);
    transfer.WriteInt32(STREAM_TRANSFER);
    transfer.WriteString(_assetName);
    transfer.WriteInt32(size);
    transfer.WriteInt32(chunkSize);
    transfer.SetId(_sub->Id());
    transfer.Prepare();
    _conn->WriteMessage(transfer);

    while (_sent > 0 && !_error)
    {
        _uploadStream.read((char*)buffer, chunkSize);

        const int numBytes = std::min(chunkSize, size);

        Message msg(_nodeName, _messageName);
        msg.WriteInt32(STREAM_DATA);
        msg.WriteBytes(numBytes, buffer);
        msg.SetId(_sub->Id());
        msg.Prepare();
        _conn->WriteMessage(msg);

        _sent -= numBytes;
    }

    if (!_error)
    {
        Message close(_nodeName, _messageName);
        close.WriteInt32(STREAM_END);
        close.SetId(_sub->Id());
        close.Prepare();
        _conn->WriteMessage(close);

        _isUploading = false;
        _uploadStream.close();
        _uploadCallback();
        _conn->RemoveSubscriber(_sub);
    }
}

void UploadStream::OnStatus(Message& msg)
{
    const int status = msg.ReadInt32();
    if (status == STATUS_ERROR)
    {
        _isUploading = false;
        _error = true;
        _uploadCallback();
        _conn->RemoveSubscriber(_sub);
    }
    else if (status == STATUS_OK)
    {
        _thread = std::thread(&UploadStream::UploadThread, this);
    }
}

void UploadStream::Upload(const std::string& asset, const std::function<void()>& callback)
{
    if (!_isUploading)
    {
        const std::string path = _dir + "/" + asset;

        _uploadCallback = callback;
        _uploadStream = std::ifstream(path, std::ios::binary);
        if (_uploadStream.good())
        {
            _isUploading = true;
            _error = false;

            std::wstring_convert<std::codecvt_utf8_utf16<wchar_t> > converter;
            const int size = GetFileSize(converter.from_bytes(path));

            if (size > 0)
            {
                _sent = size;
                _assetName = asset;

                Message msg(_nodeName, _messageName);
                msg.WriteInt32(STREAM_OPEN);
                msg.WriteString(asset);
                msg.WriteInt32(size);

                _sub = std::make_shared<Subscriber>(msg);
                _conn->AddSubscriber(_sub);
                _sub->Signal([this, sub2 = _sub](Oxygen::Message& msg)
                    {
                        const int type = msg.ReadInt32();
                        switch (type)
                        {
                        case STREAM_STATUS:
                            OnStatus(msg);
                            break;
                        case STREAM_PROTOCOL_ERROR:
                            _error = true;
                            _thread.join();
                            _uploadCallback();
                            _conn->RemoveSubscriber(sub2);
                            break;
                        }

                    });
            }
        }
    }
}
