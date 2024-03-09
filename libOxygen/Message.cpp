#include "Message.h"

using namespace Oxygen;


Message::Message(unsigned char* data, int size)
    :
    _data(data, data + size),
    _it(_data.begin())
{
    _nodeName = ReadString();
    _messageName = ReadString();
}

Message::Message(const std::string& nodeName, const std::string& messageName)
    : _nodeName(nodeName), _messageName(messageName)
{
    // Reverse space for the header bytes.
    _data.push_back(0);
    _data.push_back(0);
    _data.push_back(0);
    _data.push_back(0);

    WriteString(nodeName);
    WriteString(messageName);
}

Message::Message(const Message& msg)
    :
    _data(msg._data),
    _nodeName(msg._nodeName),
    _messageName(msg._messageName)
{
    if (msg._it._Ptr)
    {
        _it = _data.begin() + (msg._it - msg._data.begin());
    }
}

void Message::WriteString(const std::string& str)
{
    const unsigned int length = str.size();

    _data.push_back(length & 0xFF);
    _data.push_back((length >> 8) & 0xFF);
    _data.push_back((length >> 16) & 0xFF);
    _data.push_back((length >> 24) & 0xFF);
    for (int i = 0; i < length; i++)
    {
        _data.push_back(str[i] & 0xff);
    }
}

void Message::WriteBytes(int numBytes, unsigned char* bytes)
{
    _data.push_back(numBytes & 0xFF);
    _data.push_back((numBytes >> 8) & 0xFF);
    _data.push_back((numBytes >> 16) & 0xFF);
    _data.push_back((numBytes >> 24) & 0xFF);
    for (int i = 0; i < numBytes; i++)
    {
        _data.push_back(bytes[i] & 0xff);
    }
}

const std::string Message::ReadString()
{
    int a = *(_it++);
    int b = *(_it++);
    int c = *(_it++);
    int d = *(_it++);

    int numChars = a |
        (b << 8) |
        (c << 16) |
        (d << 24);
    char* str = (char*) _it._Ptr;
    _it += numChars;
    return std::string(str, numChars);
}

int Message::ReadInt32()
{
    int a = *(_it++);
    int b = *(_it++);
    int c = *(_it++);
    int d = *(_it++);

    return a |
        (b << 8) |
        (c << 16) |
        (d << 24);
}

double Message::ReadDouble()
{
    int64_t a = *(_it++);
    int64_t b = *(_it++);
    int64_t c = *(_it++);
    int64_t d = *(_it++);
    int64_t e = *(_it++);
    int64_t f = *(_it++);
    int64_t g = *(_it++);
    int64_t h = *(_it++);

    const int64_t val = a |
        (b << 8) |
        (c << 16) |
        (d << 24) |
        (e << 32) |
        (f << 40) |
        (g << 48) |
        (h << 56);

    return *reinterpret_cast<const double*>(&val);
}

void Message::ReadBytes(int numBytes, unsigned char* bytes)
{
    std::memcpy(bytes, _it._Ptr, numBytes);
    _it += numBytes;
}

void Message::WriteInt32(int value)
{
    _data.push_back(value & 0xFF);
    _data.push_back((value >> 8) & 0xFF);
    _data.push_back((value >> 16) & 0xFF);
    _data.push_back((value >> 24) & 0xFF);
}

void Message::WriteDouble(double value)
{
    int64_t* val = reinterpret_cast<int64_t*>(&value);
    _data.push_back(*val & 0xFF);
    _data.push_back((*val >> 8) & 0xFF);
    _data.push_back((*val >> 16) & 0xFF);
    _data.push_back((*val >> 24) & 0xFF);
    _data.push_back((*val >> 32) & 0xFF);
    _data.push_back((*val >> 40) & 0xFF);
    _data.push_back((*val >> 48) & 0xFF);
    _data.push_back((*val >> 56) & 0xFF);
}

void Message::Prepare()
{
    const int payloadSize = _data.size() - 4;
    _data[0] = (payloadSize & 0xFF);
    _data[1] = ((payloadSize >> 8) & 0xFF);
    _data[2] = ((payloadSize >> 16) & 0xFF);
    _data[3] = ((payloadSize >> 24) & 0xFF);
}

