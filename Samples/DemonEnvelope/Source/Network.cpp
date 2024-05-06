#include "Network.h"
#include "ClientConnection.h"
#include "Subscriber.h"
#include "Level.h"
#include "DeltaCompress.h"
#include "EventStream.h"
#include "ObjectStream.h"
#include <unordered_map>
#include <iostream>

using namespace DE;

void Network::Connect(const std::string& hostname)
{
    conn = new Oxygen::ClientConnection(hostname, Oxygen::DEFAULT_PORT);
    _state = Network_State::Connected;
}

void Network::StartAssetService(const std::string& assetDir)
{
    if (_state == Network_State::Connected)
    {
        _assetService = std::make_unique<Oxygen::AssetService>(conn, assetDir);
    }
}

void Network::GetAssets(std::vector<Asset>& assets)
{
    if (_state == Network_State::LoggedIn ||
        _state == Network_State::JoinedLevel)
    {
        _assetService->GetAssetList([this, &assets](std::vector<std::string>& assetList)
            {
                std::unordered_map<std::string, int> map;
                int index = 0;
                for (auto& item : assets)
                {
                    map.insert(std::make_pair(item.name, index));
                    index++;
                }

                for (auto& asset : assetList)
                {
                    const auto& it = map.find(asset);
                    if (it != map.end())
                    {
                        assets[it->second].onServer = true;
                    }
                    else
                    {
                        Asset ass = {};
                        ass.name = asset;
                        ass.onDisk = false;
                        ass.onServer = true;
                        assets.push_back(ass);
                    }
                }
            });
    }
}

void Network::Login(const std::string& username, const std::string& password, std::vector<Asset>& assets, std::vector<std::string>& levels)
{
    if (_state == Network_State::Connected)
    {
        conn->Logon(username, password);
        conn->LogonHandler([this, &levels, &assets](int code, const std::string& text)
            {
                if (code == 0)
                {
                    _state = Network_State::LoggedIn;

                    // Request initial data.
                    ListLevels(levels);
                    GetAssets(assets);

                    _metrics = std::make_unique<Oxygen::Metrics>(conn);
                }
                else
                {
                    std::cout << code << " " << text << std::endl;
                    disconnect = true;
                    _state = Network_State::Disconnected;
                }
            });

        _state = Network_State::LoggingIn;
    }
}

void Network::CreateLevel(const std::string& name, std::shared_ptr<Level>& level)
{
    if (_state == Network_State::LoggedIn ||
        _state == Network_State::JoinedLevel)
    {
        Oxygen::Message request("LEVEL_SVR", "NEW_LEVEL");
        request.WriteString(name);

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
        sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub), name2 = name, &level](Oxygen::Message& msg) {
            if (msg.ReadString() == "NACK")
            {
                std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
            }
            else
            {
                JoinLevel(name2, level);
            }
            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);
    }
}

void Network::JoinLevel(const std::string& name, std::shared_ptr<Level>& level)
{
    if (_state == Network_State::LoggedIn)
    {
        Oxygen::Message request("LEVEL_SVR", "LOAD_LEVEL");
        request.WriteString(name);

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
        sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub), &level](Oxygen::Message& msg) {
            if (msg.ReadString() == "NACK")
            {
                std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
                _state = Network_State::LoggedIn;
            }
            else
            {
                _state = Network_State::JoinedLevel;
                OnLevelLoaded(level);
            }
            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);

        _state = Network_State::JoiningLevel;
    }
}

void Network::ObjectStreamClosed()
{
    conn->RemoveSubscriber(levelSub);
    levelSub.reset();
}

void Network::EventStreamClosed()
{
    conn->RemoveSubscriber(eventSub);
    eventSub.reset();
}

void Network::DownloadAsset(const std::string& asset)
{
    _assetService->DownloadAsset(asset, [this]() {
        std::cout << "Download completed" << std::endl;
        });
}

void Network::UploadAsset(const std::string& asset)
{
    _assetService->UploadAsset(asset, [this]() {
        std::cout << "Upload completed" << std::endl;
        });
}

void Network::CloseLevel()
{
    if (_state == Network_State::JoinedLevel)
    {
        Oxygen::Message request("LEVEL_SVR", "CLOSE_LEVEL");

        closeSub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
        closeSub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(closeSub)](Oxygen::Message& msg) {
            if (msg.ReadString() == "NACK")
            {
                std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
            }
            else
            {
                _state = Network_State::LoggedIn;
            }
            conn->RemoveSubscriber(sub2);
            closeSub.reset();
            });
        conn->AddSubscriber(closeSub);
    }
}

void Network::ListLevels(std::vector<std::string>& levels)
{
    if (_state == Network_State::LoggedIn ||
        _state == Network_State::JoinedLevel)
    {
        Oxygen::Message request("LEVEL_SVR", "LIST_LEVELS");

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
        sub->Signal([this, &levels, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& msg) {
            if (msg.ReadString() == "NACK")
            {
                std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
            }
            else
            {
                int numLevels = msg.ReadInt32();
                for (int i = 0; i < numLevels; i++)
                {
                    levels.push_back(msg.ReadString());
                }
            }
            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);
    }
}

void Network::OnLevelLoaded(std::shared_ptr<Level>& level)
{
    level->Loaded();

    levelSub = std::shared_ptr<Oxygen::ObjectStream>(new DE::ObjectStream(level, *this));
    conn->AddSubscriber(levelSub);
    std::cout << "Opening Object Stream" << std::endl;

    eventSub = std::shared_ptr<Oxygen::EventStream>(new DE::EventStream(*this));
    conn->AddSubscriber(eventSub);
    std::cout << "Opening Event Stream" << std::endl;
}

void Network::CreateTilemap(int width, int height, int numLayers)
{
    Tilemap tilemap;
    tilemap.Load(0, width, height);
    tilemap.CreateLayers(numLayers);

    Oxygen::Object obj = {};
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;

    {
        Oxygen::Message msg = levelSub->BuildAddMessage(obj);
        msg.WriteString("TILEMAP");
        msg.WriteInt32(width);
        msg.WriteInt32(height);
        msg.WriteInt32(tilemap.NumLayers());

        levelSub->PrepareAddMessage(&msg, obj);

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg));
        sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& response) {
            if (response.ReadString() == "NACK")
            {
                std::cout << response.ReadInt32() << " " << response.ReadString() << std::endl;
            }

            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);
    }

    for (int i = 0; i < numLayers; i++)
    {
        auto& layer = tilemap.GetLayer(i);
        Oxygen::Message msg = levelSub->BuildAddMessage(obj);
        msg.WriteString("TILEMAP_LAYER");
        msg.WriteInt32(layer.Layer());
        layer.Serialize(msg);

        levelSub->PrepareAddMessage(&msg, obj);

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg));
        sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& response) {
            if (response.ReadString() == "NACK")
            {
                std::cout << response.ReadInt32() << " " << response.ReadString() << std::endl;
            }

            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);
    }

    {
        auto& mask = tilemap.GetCollisionMask();
        Oxygen::Message msg = levelSub->BuildAddMessage(obj);
        msg.WriteString("TILEMAP_MASK");
        mask.Serialize(msg);

        levelSub->PrepareAddMessage(&msg, obj);

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg));
        sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& response) {
            if (response.ReadString() == "NACK")
            {
                std::cout << response.ReadInt32() << " " << response.ReadString() << std::endl;
            }

            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);
    }
}

void Network::UpdateTilemask(const Tilemap_Mask& mask)
{
    Oxygen::Object obj = {};
    obj.id = mask.ID();
    obj.version = mask.Version();
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;

    Oxygen::Message msg = levelSub->BuildUpdateMessage(obj);
    msg.WriteString("TILEMAP_MASK");
    mask.Serialize(msg);

    SendUpdateMsg(msg, obj);
}

void Network::UpdateTilemap(const Tilemap_Layer& layer)
{
    Oxygen::Object obj = {};
    obj.id = layer.ID();
    obj.version = layer.Version();
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;

    Oxygen::Message msg = levelSub->BuildUpdateMessage(obj);
    msg.WriteString("TILEMAP_LAYER");
    msg.WriteInt32(layer.Layer());
    layer.Serialize(msg);

    SendUpdateMsg(msg, obj);
}

void Network::CreateScript(int parentId, int x, int y)
{
    Oxygen::Object obj = {};
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;

    {
        Oxygen::Message msg = levelSub->BuildAddMessage(obj);
        msg.WriteString("SCRIPT");

        ScriptObject sc(-1);
        sc.SetX(x);
        sc.SetY(y);
        sc.SetParentID(parentId);
        sc.Serialize(msg);

        levelSub->PrepareAddMessage(&msg, obj);

        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg));
        sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& response) {
            if (response.ReadString() == "NACK")
            {
                std::cout << response.ReadInt32() << " " << response.ReadString() << std::endl;
            }

            conn->RemoveSubscriber(sub2);
            });
        conn->AddSubscriber(sub);
    }
}

void Network::UpdateScript(ScriptObject& script)
{
    Oxygen::Object obj = {};
    obj.id = script.ID();
    obj.version = script.Version();
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;

    Oxygen::Message msg = levelSub->BuildUpdateMessage(obj);
    msg.WriteString("SCRIPT");
    script.Serialize(msg);

    SendUpdateMsg(msg, obj);
}

void Network::UpdateCursor(int objectId, int subID)
{
    Oxygen::Message msg("LEVEL_SVR", "UPDATE_CURSOR");
    msg.WriteInt32(objectId);
    msg.WriteInt32(subID);

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg));
    sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& response) {
        if (response.ReadString() == "NACK")
        {
            std::cout << response.ReadInt32() << " " << response.ReadString() << std::endl;
        }

        conn->RemoveSubscriber(sub2);
        });
    conn->AddSubscriber(sub);
}

void Network::SendUpdateMsg(Oxygen::Message& msg, Oxygen::Object& obj)
{
    levelSub->PrepareUpdateMessage(&msg, obj);

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg));
    sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& response) {
        if (response.ReadString() == "NACK")
        {
            std::cout << response.ReadInt32() << " " << response.ReadString() << std::endl;
        }

        conn->RemoveSubscriber(sub2);
        });
    conn->AddSubscriber(sub);
}

bool Network::Connected()
{
    return conn && conn->IsConnected();
}

void Network::Process()
{
    if (disconnect)
    {
        delete conn;
        conn = nullptr;
        disconnect = false;
    }

    if (conn)
    {
        conn->Process(false);
    }
}
