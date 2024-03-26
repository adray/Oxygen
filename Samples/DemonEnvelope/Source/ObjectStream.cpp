#include <iostream>
#include "ObjectStream.h"
#include "Level.h"
#include "Network.h"

using namespace DE;

ObjectStream::ObjectStream(std::shared_ptr<Level>& level, Network& network)
    : 
    _level(level),
    _network(network)
{

}

void ObjectStream::OnNewObject(const Oxygen::Object& ev, Oxygen::Message& msg)
{
    const std::string name = msg.ReadString();

    if (name == "TILEMAP")
    {
        auto& tilemap = _level->GetTilemap();
        tilemap.SetID(ev.id);
        _level->CreateTilemap();
        tilemap.Deserialize(msg);

        std::cout << "Tilemap num tiles: " << tilemap.NumTiles() << std::endl;
    }
}

void ObjectStream::OnUpdateObject(const Oxygen::Object& ev, Oxygen::Message& msg)
{
    const std::string name = msg.ReadString();

    if (name == "TILEMAP")
    {
        auto& tilemap = _level->GetTilemap();
        if (tilemap.ID() == ev.id)
        {
            tilemap.Deserialize(msg);

            std::cout << "Tilemap num tiles: " << tilemap.NumTiles() << std::endl;
        }
    }
}

void ObjectStream::OnStreamEnded()
{
    std::cout << "Object stream closed" << std::endl;
    _network.ObjectStreamClosed();
}

ObjectStream::~ObjectStream()
{

}
