#pragma once
#include <string>
#include <vector>

namespace Oxygen
{
    class ClientConnection;
    class Message;
}

namespace DE
{
    class Level;
    class Tilemap;

    class Network
    {
    public:
        void Connect(const std::string& hostname);
        void Login(const std::string& username, const std::string& password);
        void GetAssets(std::vector<std::string>& assets);
        void CreateLevel(const std::string& name, Level& level);
        void JoinLevel(const std::string& name, Level& level);
        void ListLevels(std::vector<std::string>& levels);
        void CreateTilemap(int width, int height);
        void UpdateTilemap(Tilemap& tilemap, const std::vector<unsigned char>& stateData);
        bool Connected();
        void Process();
    private:

        void OnLevelLoaded(Level& level);
        void OnObjectStreamed(Level& level, Oxygen::Message& msg);

        Oxygen::ClientConnection* conn;
    };
}
