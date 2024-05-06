#include "DeltaCompress.h"

using namespace Oxygen;

constexpr char UNCOMPRESSED_BLOCK = 0;
constexpr char DELTA_COMPRESSED_BLOCK = 1;

static void CalculateDeltas(
    const unsigned char* initialData,
    const unsigned char* newData,
    int initialDataOffset, 
    int newDataOffset,
    int newDataLength,
    std::vector<unsigned char> &delta)
{
    for (int i = 0; i < newDataLength; i++)
    {
        delta.push_back((unsigned char)(initialData[i + initialDataOffset] - newData[i + newDataOffset]));
    }
}

static void WriteBlock(
    std::vector<unsigned char>& stream,
    const unsigned char* data,
    int dataLength,
    char flags,
    int offset)
{
    stream.push_back(flags);

    if (flags == DELTA_COMPRESSED_BLOCK)
    {
        stream.push_back((unsigned char)(dataLength & 0xFF));
        stream.push_back((unsigned char)((dataLength >> 8) & 0xFF));

        // Then run length encode the data.
        unsigned char value = data[0];
        int count = 1;
        for (int i = 1; i < dataLength; i++)
        {
            if (data[i] == value)
            {
                count++;
            }
            else
            {
                stream.push_back(value);
                stream.push_back((unsigned char)(count & 0xFF));
                stream.push_back((unsigned char)((count >> 8) & 0xFF));

                value = data[i];
                count = 1;
            }
        }

        stream.push_back(value);
        stream.push_back((unsigned char)(count & 0xFF));
        stream.push_back((unsigned char)((count >> 8) & 0xFF));
    }
    else if (flags == UNCOMPRESSED_BLOCK)
    {
        stream.push_back((unsigned char)(dataLength & 0xFF));
        stream.push_back((unsigned char)((dataLength >> 8) & 0xFF));

        for (int i = 0; i < dataLength; i++)
        {
            stream.push_back((unsigned char)data[offset + i]);
        }
    }
}

static int FindSubArray(const unsigned char* longer, const unsigned char* shorter, int longerLength, int shorterLength)
{
    if (longerLength <= shorterLength)
    {
        return -2;
    }

    for (int i = 0; i < longerLength; i++)
    {
        bool success = true;
        for (int j = 0; j < shorterLength; j++)
        {
            if (longer[i + j] != shorter[j])
            {
                success = false;
                break;
            }
        }

        if (success)
        {
            return i;
        }
    }

    return -1;
}

static void WriteData(std::vector<unsigned char>& stream,
    const unsigned char* longer, const unsigned char* shorter,
    int longerLength, int shorterLength, int offset)
{
    int length = shorterLength;
    std::vector<unsigned char> delta;
    CalculateDeltas(longer, shorter, length, offset, 0, delta);

    if (offset > 0)
    {
        const int uncompressedLength = offset;
        WriteBlock(stream, longer, UNCOMPRESSED_BLOCK, 0, uncompressedLength);
    }

    WriteBlock(stream, delta.data(), int(delta.size()), DELTA_COMPRESSED_BLOCK, offset);

    if (offset + length < longerLength)
    {
        const int uncompressedLength = offset + length;
        WriteBlock(stream, longer, UNCOMPRESSED_BLOCK, uncompressedLength, longerLength - uncompressedLength);
    }
}

int Oxygen::Compress(const unsigned char* initialData, int numInitialBytes, const unsigned char* newData, int numNewDataBytes, unsigned char** deltaData)
{
    std::vector<unsigned char> stream;
    std::vector<unsigned char> delta;

    if (numInitialBytes > numNewDataBytes)
    {
        WriteBlock(stream, newData, numNewDataBytes, UNCOMPRESSED_BLOCK, 0);
    }
    else if (numNewDataBytes > numInitialBytes)
    {
        int offset = FindSubArray(newData, initialData, numNewDataBytes, numInitialBytes);
        if (offset > -1)
        {
            WriteData(stream, newData, initialData, numNewDataBytes, numInitialBytes, offset);
        }
        else
        {
            WriteBlock(stream, newData, numNewDataBytes, UNCOMPRESSED_BLOCK, 0);
        }
    }
    else
    {
        CalculateDeltas(initialData, newData, 0, 0, numNewDataBytes, delta);
        WriteBlock(stream, delta.data(), int(delta.size()), DELTA_COMPRESSED_BLOCK, 0);
    }

    *deltaData = new unsigned char[stream.size()];
    std::memcpy(*deltaData, stream.data(), stream.size());

    return int(stream.size());
}

static void ReadBlock(
    std::vector<unsigned char>& newData,
    const unsigned char* initialData,
    const unsigned char* delta,
    int numInitialData,
    int& pos)
{
    int flags = delta[pos++];
    if (flags == UNCOMPRESSED_BLOCK)
    {
        int countLo = delta[pos++];
        int countHi = delta[pos++];

        int count = countLo | (countHi << 8);

        for (int i = 0; i < count; i++)
        {
            newData.push_back(delta[pos++]);
        }
    }
    else if (flags == DELTA_COMPRESSED_BLOCK)
    {
        int dataLength;
        {
            int countLo = delta[pos++];
            int countHi = delta[pos++];

            dataLength = countLo | (countHi << 8);
        }

        // Decode the run length encoding.
        size_t offset = newData.size();
        int end = int(newData.size()) + dataLength;
        while (newData.size() < end)
        {
            int value = delta[pos++];
            int countLo = delta[pos++];
            int countHi = delta[pos++];

            int count = countLo | (countHi << 8);

            for (int j = 0; j < count; j++)
            {
                newData.push_back((char)value);
            }
        }

        // Decode the deltas.
        for (int i = 0; i < numInitialData; i++)
        {
            newData[i + offset] = (char)(initialData[i] - newData[i + offset]);
        }
    }
}

void Oxygen::Decompress(unsigned char* initialData, int numInitialBytes, unsigned char* delta, int numDeltaBytes, std::vector<unsigned char>& newData)
{
    int pos = 0;
    while (pos < numDeltaBytes)
    {
        ReadBlock(newData, initialData, delta, numInitialBytes, pos);
    }
}
