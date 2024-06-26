#pragma once
#include <string>
#include <vector>
#include <memory>
#include "Metrics.h"
#include "AssetService.h"
#include "Asset.h"
#include "PluginService.h"
#include "BuildService.h"

namespace Oxygen
{
    class ClientConnection;
    class Message;
    class Subscriber;
    class ObjectStream;
    class EventStream;
    struct Object;
}

namespace DE
{
    class Level;
    class Tilemap;
    class Tilemap_Layer;
    class Tilemap_Mask;
    class ScriptObject;
    class NPCObject;

    enum class Network_State
    {
        Disconnected,
        Connected,
        LoggingIn,
        LoggedIn,
        JoiningLevel,
        JoinedLevel
    };

    class Network
    {
    public:
        void Connect(const std::string& hostname);
        void Login(const std::string& username, const std::string& password, std::vector<Asset>& assets, std::vector<std::string>& levels);
        void GetAssets(std::vector<Asset>& assets);
        void CreateLevel(const std::string& name, std::shared_ptr<Level>& level);
        void JoinLevel(const std::string& name, std::shared_ptr<Level>& level);
        void DeleteLevel(const std::string& name);
        void CloseLevel();
        void ListLevels(std::vector<std::string>& levels);
        void CreateTilemap(int width, int height, int numLayers);
        void UpdateTilemap(const Tilemap_Layer& layer);
        void UpdateTilemask(const Tilemap_Mask& mask);
        void UpdateCursor(int objectId, int subID);
        void CreateNPC(int parentId, int x, int y);
        void UpdateNPC(NPCObject& npc);
        void DeleteNPC(int id);
        void CreateScript(int parentId, int x, int y);
        void UpdateScript(ScriptObject& script);
        void DeleteObject(int id);
        bool Connected();
        inline Network_State State() const { return _state; }
        void Process();
        void ObjectStreamClosed();
        void EventStreamClosed();
        void StartAssetService(const std::string& assetDir);
        void DownloadAsset(const std::string& asset);
        void UploadAsset(const std::string& asset);
        void BakeAssets();
        bool IsBaking();

        inline std::shared_ptr<Oxygen::EventStream> EventStream() const { return eventSub; }
        inline std::shared_ptr<Oxygen::Subscriber> CloseSub() const { return closeSub; }
    private:

        void OnLevelLoaded(std::shared_ptr<Level>& level);
        void SendMsg(Oxygen::Message& msg);
        void SendUpdateMsg(Oxygen::Message& msg, Oxygen::Object& obj);

        Oxygen::ClientConnection* conn;
        std::shared_ptr<Oxygen::Subscriber> closeSub;
        std::shared_ptr<Oxygen::ObjectStream> levelSub;
        std::shared_ptr<Oxygen::EventStream> eventSub;
        bool disconnect = false;
        Network_State _state = Network_State::Disconnected;
        std::unique_ptr<Oxygen::Metrics> _metrics;
        std::unique_ptr<Oxygen::AssetService> _assetService;
        std::unique_ptr<Oxygen::PluginService> _pluginService;
        std::unique_ptr<Oxygen::BuildService> _buildService;
    };
}
