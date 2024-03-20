#include "DeltaCompress.h"

using namespace Oxygen;


int Oxygen::Compress(const unsigned char* initialData, int numInitialBytes, const unsigned char* newData, int numNewDataBytes, unsigned char** deltaData)
{
    if (numInitialBytes != numNewDataBytes)
    {
        return 0;
    }

    // First calculate deltas.
    std::vector<unsigned char> delta(std::max(numInitialBytes, numNewDataBytes));
    for (int i = 0; i < numNewDataBytes; i++)
    {
        delta[i] = (unsigned char)(initialData[i] - newData[i]);
    }

    if (numNewDataBytes > numInitialBytes)
    {
        std::memcpy(delta.data() + numInitialBytes, newData + numInitialBytes, numNewDataBytes - numInitialBytes);
    }
    else if (numInitialBytes > numNewDataBytes)
    {
        std::memcpy(delta.data() + numNewDataBytes, initialData + numNewDataBytes, numInitialBytes - numNewDataBytes);
    }

    // Then run length encode the data.
    std::vector<unsigned char> stream;
    unsigned char value = delta[0];
    int count = 1;
    for (int i = 1; i < delta.size(); i++)
    {
        if (delta[i] == value)
        {
            count++;
        }
        else
        {
            stream.push_back(value);
            stream.push_back((unsigned char)(count & 0xFF));
            stream.push_back((unsigned char)((count >> 8) & 0xFF));

            value = delta[i];
            count = 1;
        }
    }

    stream.push_back(value);
    stream.push_back((unsigned char)(count & 0xFF));
    stream.push_back((unsigned char)((count >> 8) & 0xFF));

    *deltaData = new unsigned char[stream.size()];
    std::memcpy(*deltaData, stream.data(), stream.size());

    return int(stream.size());
}

void Oxygen::Decompress(unsigned char* initialData, int numInitialBytes, unsigned char* delta, int numDeltaBytes, std::vector<unsigned char>& newData)
{
    // Decode the run length encoding.
    
    for (int i = 0; i < numDeltaBytes; i += 3)
    {
        int value = delta[i];
        int countLo = delta[i + 1];
        int countHi = delta[i + 2];

        int count = countLo | (countHi << 8);

        for (int j = 0; j < count; j++)
        {
            newData.push_back(static_cast<unsigned char>(value));
        }
    }

    // Decode the deltas.
    for (int i = 0; i < newData.size(); i++)
    {
        newData[i] = (unsigned char)(initialData[i] - newData[i]);
    }
}
