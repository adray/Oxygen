#pragma once
#include <vector>

namespace Oxygen
{
    unsigned char* Compress(unsigned char* initialData, int numInitialBytes, unsigned char* newData, int numNewDataBytes);
    void Decompress(unsigned char* initialData, int numInitialBytes, unsigned char* delta, int numDeltaBytes, std::vector<unsigned char>& newData);
}
