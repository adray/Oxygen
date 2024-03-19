#include "DeltaCompress.h"

using namespace Oxygen;


unsigned char* Oxygen::Compress(unsigned char* initialData, int numInitialBytes, unsigned char* newData, int numNewDataBytes)
{
    return nullptr;
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
