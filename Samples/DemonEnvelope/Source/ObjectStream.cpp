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
        _level->CreateTilemap();

        auto& tilemap = _level->GetTilemap();
        const int width = msg.ReadInt32();
        const int height = msg.ReadInt32();
        tilemap.Load(width, height);

        const int numLayers = msg.ReadInt32();
        tilemap.CreateLayers(numLayers);
    }
    else if (name == "TILEMAP_LAYER")
    {
        const int layer = msg.ReadInt32();
        auto& tilemap = _level->GetTilemap();
        
        tilemap.GetLayer(layer).Deserialize(msg);
        tilemap.GetLayer(layer).SetID(ev.id);

        std::cout << "Tilemap num tiles: " << tilemap.NumTiles() << std::endl;
    }
    else if (name == "TILEMAP_MASK")
    {
        auto& tilemap = _level->GetTilemap();
        auto& mask = tilemap.GetCollisionMask();
        mask.Deserialize(msg);
        mask.SetID(ev.id);
    }
}

void ObjectStream::OnUpdateObject(const Oxygen::Object& ev, Oxygen::Message& msg)
{
    const std::string name = msg.ReadString();

    if (name == "TILEMAP_LAYER")
    {
        const int index = msg.ReadInt32();

        auto& tilemap = _level->GetTilemap();
        
        auto& layer = tilemap.GetLayer(index);
        if (layer.ID() == ev.id)
        {
            layer.SetVersion(ev.version);
            layer.Deserialize(msg);

            std::cout << "Tilemap num tiles: " << tilemap.NumTiles() << std::endl;
        }
        else
        {
            std::cout << "Error: Tilemap layer id out of sync" << std::endl;
        }
    }
    else if (name == "TILEMAP_MASK")
    {
        auto& tilemap = _level->GetTilemap();
        auto& mask = tilemap.GetCollisionMask();
        if (mask.ID() == ev.id)
        {
            mask.SetVersion(ev.version);
            mask.Deserialize(msg);
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
