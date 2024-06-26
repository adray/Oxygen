#include "Message.h"

using namespace Oxygen;


Message::Message(unsigned char* data, int size)
    :
    _data(data, data + size),
    _it(_data.begin()),
    _id(-1)
{
    _nodeName = ReadString();
    _messageName = ReadString();
}

Message::Message(const std::string& nodeName, const std::string& messageName)
    : _nodeName(nodeName), _messageName(messageName), _id(-1)
{
    // Reverse space for the header bytes.
    // Size
    _data.push_back(0);
    _data.push_back(0);
    _data.push_back(0);
    _data.push_back(0);

    // Id
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
    _messageName(msg._messageName),
    _id(msg._id)
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

void Message::WriteBytes(int numBytes, const unsigned char* bytes)
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

std::int64_t Message::ReadInt64()
{
    std::int64_t a = *(_it++);
    std::int64_t b = *(_it++);
    std::int64_t c = *(_it++);
    std::int64_t d = *(_it++);
    std::int64_t e = *(_it++);
    std::int64_t f = *(_it++);
    std::int64_t g = *(_it++);
    std::int64_t h = *(_it++);

    return a |
        (b << 8) |
        (c << 16) |
        (d << 24) |
        (e << 32) |
        (f << 40) |
        (g << 48) |
        (h << 56);
}

double Message::ReadDouble()
{
    std::int64_t a = *(_it++);
    std::int64_t b = *(_it++);
    std::int64_t c = *(_it++);
    std::int64_t d = *(_it++);
    std::int64_t e = *(_it++);
    std::int64_t f = *(_it++);
    std::int64_t g = *(_it++);
    std::int64_t h = *(_it++);

    const std::int64_t val = a |
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
    std::int64_t* val = reinterpret_cast<std::int64_t*>(&value);
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
    const int payloadSize = _data.size() - 8;
    _data[0] = (payloadSize & 0xFF);
    _data[1] = ((payloadSize >> 8) & 0xFF);
    _data[2] = ((payloadSize >> 16) & 0xFF);
    _data[3] = ((payloadSize >> 24) & 0xFF);

    _data[4] = (_id & 0xFF);
    _data[5] = ((_id >> 8) & 0xFF);
    _data[6] = ((_id >> 16) & 0xFF);
    _data[7] = ((_id >> 24) & 0xFF);
}

