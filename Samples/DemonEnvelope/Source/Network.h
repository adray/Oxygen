#pragma once
#include <string>
#include <vector>
#include <memory>

namespace Oxygen
{
    class ClientConnection;
    class Message;
    class Subscriber;
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
        void CloseLevel();
        void ListLevels(std::vector<std::string>& levels);
        void CreateTilemap(int width, int height);
        void UpdateTilemap(Tilemap& tilemap, const std::vector<unsigned char>& stateData);
        bool Connected();
        inline bool LoggedIn() const { return loggedIn; }
        void Process();
    private:

        void OnLevelLoaded(Level& level);
        void OnObjectStreamed(Level& level, Oxygen::Message& msg);

        Oxygen::ClientConnection* conn;
        std::shared_ptr<Oxygen::Subscriber> levelSub;
        bool disconnect = false;
        bool loggedIn = false;
    };
}
