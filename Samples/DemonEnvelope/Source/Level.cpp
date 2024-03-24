#include "Level.h"
#include "Message.h"
#include "DeltaCompress.h"
#include <iostream>

using namespace DE;

void Level::Setup(ISLANDER_POLYGON_LIBRARY lib, ISLANDER_DEVICE device)
{
    _lib = lib;

    tileset = std::shared_ptr<Tileset>(new Tileset());
    tileset->Load(device);
}

void Level::Loaded()
{
}

void Level::OnNewObject(const Oxygen::Object& ev, Oxygen::Message& msg)
{
    const std::string name = msg.ReadString();

    if (name == "TILEMAP")
    {
        _tilemaps.SetID(ev.id);
        _tilemaps.CreateMesh(_lib, 10, 10, 16, 9);
        _tilemaps.SetTileSet(tileset);
        _tilemaps.Deserialize(msg);

        std::cout << "Tilemap num tiles: " << _tilemaps.NumTiles() << std::endl;
    }
}

void Level::OnUpdateObject(const Oxygen::Object& ev, Oxygen::Message& msg)
{
    const std::string name = msg.ReadString();

    if (name == "TILEMAP")
    {
        if (_tilemaps.ID() == ev.id)
        {
            _tilemaps.Deserialize(msg);

            std::cout << "Tilemap num tiles: " << _tilemaps.NumTiles() << std::endl;
        }
    }
}

void Level::Render(IslanderRenderable* renderables, int* cur_index, const int tilemappixelShader, const int tilemapvertexShader)
{
    IslanderRenderable* renderable = &renderables[*cur_index];
    std::memset(renderable, 0, sizeof(IslanderRenderable));

    if (_tilemaps.ID() >= 0)
    {
        renderable->mesh.pixelShader = tilemappixelShader;
        renderable->mesh.vertexShader = tilemapvertexShader;
        renderable->mesh.geometryShader = -1;
        renderable->mesh.polydata = _tilemaps.GetMesh();
        renderable->mesh.parentEntity = -1;
        renderable->mesh.material.slot_data[0] = _tilemaps.GetTileset()->GetTexture();
        renderable->mesh.material.slot_flags[0] = ISLANDER_RENDERABLE_MATERIAL_SLOT_CUSTOM;

        Tilemap::ConstantBuffer* buffer = _tilemaps.GetConstantBuffer();
        renderable->mesh.constantBufferData = buffer;
        renderable->mesh.constantBufferDataSize = sizeof(Tilemap::ConstantBuffer);

        _tilemaps.Update();

        (*cur_index)++;
    }
}

bool Level::TileMapHitTest(int x, int y) const
{
    if (_tilemaps.HitTest(x, y) >= 0)
    {
        return true;
    }

    return false;
}

void Level::Reset()
{
    _tilemaps.Clear();
}

