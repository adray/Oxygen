#include "Network.h"
#include "ClientConnection.h"
#include "Subscriber.h"
#include "Level.h"
#include "DeltaCompress.h"
#include "EventStream.h"
#include "ObjectStream.h"
#include <iostream>

using namespace DE;

void Network::Connect(const std::string& hostname)
{
    conn = new Oxygen::ClientConnection(hostname, Oxygen::DEFAULT_PORT);
}

void Network::GetAssets(std::vector<std::string>& assets)
{
    Oxygen::Message request("ASSET_SVR", "ASSET_LIST");
    request.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
    sub->Signal([this,&assets, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& msg) {
        if (msg.ReadString() == "NACK")
        {
            std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
        }
        else
        {
            int numAssets = msg.ReadInt32();
            for (int i = 0; i < numAssets; i++)
            {
                assets.push_back(msg.ReadString());
            }
        }
        conn->RemoveSubscriber(sub2);
        });
    conn->AddSubscriber(sub);
}

void Network::Login(const std::string& username, const std::string& password)
{
    Oxygen::Message request("LOGIN_SVR", "LOGIN");
    request.WriteString(username);
    conn->HashPassword(password, request);
    request.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
    sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& msg) {
        conn->RemoveSubscriber(sub2);
        if (msg.ReadString() == "NACK")
        {
            std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
            disconnect = true;
        }
        else
        {
            loggedIn = true;
        }
        });
    conn->AddSubscriber(sub);
}

void Network::CreateLevel(const std::string& name, std::shared_ptr<Level>& level)
{
    Oxygen::Message request("LEVEL_SVR", "NEW_LEVEL");
    request.WriteString(name);
    request.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
    sub->Signal([this,sub2=std::shared_ptr<Oxygen::Subscriber>(sub), name2=name, &level](Oxygen::Message& msg) {
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

void Network::JoinLevel(const std::string& name, std::shared_ptr<Level>& level)
{
    Oxygen::Message request("LEVEL_SVR", "LOAD_LEVEL");
    request.WriteString(name);
    request.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
    sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub),&level](Oxygen::Message& msg) {
        if (msg.ReadString() == "NACK")
        {
            std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
        }
        else
        {
            OnLevelLoaded(level);
        }
        conn->RemoveSubscriber(sub2);
        });
    conn->AddSubscriber(sub);
}

void Network::ObjectStreamClosed()
{
    conn->RemoveSubscriber(levelSub);
    levelSub.reset();
}

void Network::CloseLevel()
{
    Oxygen::Message request("LEVEL_SVR", "CLOSE_LEVEL");
    request.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
    sub->Signal([this, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& msg) {
        if (msg.ReadString() == "NACK")
        {
            std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
        }
        else
        {
            
        }
        conn->RemoveSubscriber(sub2);
        });
    conn->AddSubscriber(sub);
}

void Network::ListLevels(std::vector<std::string>& levels)
{
    Oxygen::Message request("LEVEL_SVR", "LIST_LEVELS");
    request.Prepare();

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

void Network::OnLevelLoaded(std::shared_ptr<Level>& level)
{
    level->Loaded();

    levelSub = std::shared_ptr<Oxygen::ObjectStream>(new DE::ObjectStream(level, *this));
    conn->AddSubscriber(levelSub);
    std::cout << "Opening Object Stream" << std::endl;

    eventSub = std::shared_ptr<Oxygen::EventStream>(new DE::EventStream());
    conn->AddSubscriber(eventSub);
    std::cout << "Opening Event Stream" << std::endl;
}

void Network::CreateTilemap(int width, int height)
{
    Oxygen::Object obj = {};
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;
    obj.hasCustomData = 1;

    Oxygen::Message msg = levelSub->BuildAddMessage(obj);
    msg.WriteString("TILEMAP");

    Tilemap tilemap;
    tilemap.Load(width, height);
    tilemap.Serialize(msg);

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

void Network::UpdateTilemap(Tilemap& tilemap)
{
    Oxygen::Object obj = {};
    obj.id = tilemap.ID();
    obj.version = tilemap.Version();
    obj.scale[0] = 1.0;
    obj.scale[1] = 1.0;
    obj.scale[2] = 1.0;
    obj.hasCustomData = true;

    Oxygen::Message msg = levelSub->BuildUpdateMessage(obj);
    msg.WriteString("TILEMAP");
    tilemap.Serialize(msg);

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

void Network::UpdateCursor(int objectId, int subID)
{
    Oxygen::Message msg("LEVEL_SVR", "UPDATE_CURSOR");
    msg.WriteInt32(objectId);
    msg.WriteInt32(subID);
    msg.Prepare();

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
        loggedIn = false;
    }

    if (conn)
    {
        conn->Process(false);
    }
}
