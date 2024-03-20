#include "Network.h"
#include "ClientConnection.h"
#include "Subscriber.h"
#include "Level.h"
#include "DeltaCompress.h"
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
        if (msg.ReadString() == "NACK")
        {
            std::cout << msg.ReadInt32() << " " << msg.ReadString() << std::endl;
        }

        conn->RemoveSubscriber(sub2);
        });
    conn->AddSubscriber(sub);
}

void Network::CreateLevel(const std::string& name, Level& level)
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

void Network::JoinLevel(const std::string& name, Level& level)
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

void Network::OnLevelLoaded(Level& level)
{
    level.Loaded();

    Oxygen::Message request("LEVEL_SVR", "OBJECT_STREAM");
    request.Prepare();

    std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(request));
    sub->Signal([this, &level, sub2 = std::shared_ptr<Oxygen::Subscriber>(sub)](Oxygen::Message& msg) {
        OnObjectStreamed(level, msg);
        });
    conn->AddSubscriber(sub);
}

void Network::OnObjectStreamed(Level& level, Oxygen::Message& msg)
{
    const int type = msg.ReadInt32();
    switch (type)
    {
    case 0: // ADD
        level.OnNewObject(msg);
        break;
    case 1: // UPDATE
        level.OnUpdateObject(msg);
        break;
    case 2: // DELETE
        level.OnDeleteObject(msg);
        break;
    }
}

void Network::CreateTilemap(int width, int height)
{
    Oxygen::Message msg("LEVEL_SVR", "ADD_OBJECT");
    msg.WriteDouble(0.0); // Pos X
    msg.WriteDouble(0.0); // Pos Y
    msg.WriteDouble(0.0); // Pos Z
    msg.WriteDouble(1.0); // Scale X
    msg.WriteDouble(1.0); // Scale Y
    msg.WriteDouble(1.0); // Scale Z
    msg.WriteDouble(0.0); // Rot X
    msg.WriteDouble(0.0); // Rot Y
    msg.WriteDouble(0.0); // Rot Z
    msg.WriteInt32(1);    // custom
    msg.WriteString("TILEMAP");

    Tilemap tilemap;
    tilemap.Load(width, height);
    tilemap.Serialize(msg);

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

constexpr int NEW_OBJECT = 0;
constexpr int UPDATE_OBJECT = 1;
constexpr int DELETE_OBJECT = 2;

void Network::UpdateTilemap(Tilemap& tilemap, const std::vector<unsigned char>& stateData)
{
    Oxygen::Message msg("LEVEL_SVR", "OBJECT_STREAM");
    msg.WriteInt32(NEW_OBJECT);
    msg.WriteInt32(tilemap.ID());
    msg.WriteDouble(0.0); // Pos X
    msg.WriteDouble(0.0); // Pos Y
    msg.WriteDouble(0.0); // Pos Z
    msg.WriteDouble(1.0); // Scale X
    msg.WriteDouble(1.0); // Scale Y
    msg.WriteDouble(1.0); // Scale Z
    msg.WriteDouble(0.0); // Rot X
    msg.WriteDouble(0.0); // Rot Y
    msg.WriteDouble(0.0); // Rot Z
    msg.WriteInt32(1);    // custom
    msg.WriteString("TILEMAP");
    tilemap.Serialize(msg);
    msg.Prepare();

    unsigned char* newData;
    int numBytes = Oxygen::Compress(stateData.data(), stateData.size(), msg.data()+4, msg.size()-4, &newData);
    if (numBytes > 0)
    {
        Oxygen::Message msg2("LEVEL_SVR", "UPDATE_OBJECT");
        msg2.WriteInt32(tilemap.ID());
        msg2.WriteBytes(numBytes, newData);
        delete[] newData;

        msg2.Prepare();
        std::shared_ptr<Oxygen::Subscriber> sub = std::shared_ptr<Oxygen::Subscriber>(new Oxygen::Subscriber(msg2));
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

bool Network::Connected()
{
    return conn && conn->IsConnected();
}

void Network::Process()
{
    if (conn)
    {
        conn->Process(false);
    }
}
