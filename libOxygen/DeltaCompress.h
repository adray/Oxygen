#pragma once
#include <vector>

namespace Oxygen
{
    int Compress(const unsigned char* initialData, int numInitialBytes, const unsigned char* newData, int numNewDataBytes, unsigned char** deltaData);
    void Decompress(unsigned char* initialData, int numInitialBytes, unsigned char* delta, int numDeltaBytes, std::vector<unsigned char>& newData);
}
