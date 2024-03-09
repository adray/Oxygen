#pragma once
#include "ClientConnection.h"
#include <unordered_map>
#include <vector>

class Level
{
private:
    std::unordered_map<int, std::vector<unsigned char>> state;

    void OnObjectAdded(Oxygen::Message& msg);

public:
    void OpenLevel(Oxygen::ClientConnection* conn);

    void DecompressMessage(Oxygen::Message& msg);

    void OnObjectStreamed(Oxygen::ClientConnection* conn, Oxygen::Message& msg);

    void AddObject(Oxygen::ClientConnection* conn, double* pos);

    void UpdateObject(Oxygen::ClientConnection* conn, int id, double* pos);
};
