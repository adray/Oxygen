#pragma once
#include <string>
#include <vector>
#include <memory>

namespace Oxygen
{
    class ClientConnection;
    class Message;
    class Subscriber;
    class ObjectStream;
    class EventStream;
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
        void CreateLevel(const std::string& name, std::shared_ptr<Level>& level);
        void JoinLevel(const std::string& name, std::shared_ptr<Level>& level);
        void CloseLevel();
        void ListLevels(std::vector<std::string>& levels);
        void CreateTilemap(int width, int height);
        void UpdateTilemap(Tilemap& tilemap);
        void UpdateCursor(int objectId, int subID);
        bool Connected();
        inline bool LoggedIn() const { return loggedIn; }
        void Process();
        void ObjectStreamClosed();

        inline std::shared_ptr<Oxygen::EventStream> EventStream() const { return eventSub; }
    private:

        void OnLevelLoaded(std::shared_ptr<Level>& level);

        Oxygen::ClientConnection* conn;
        std::shared_ptr<Oxygen::ObjectStream> levelSub;
        std::shared_ptr<Oxygen::EventStream> eventSub;
        bool disconnect = false;
        bool loggedIn = false;
    };
}
