#include "Level.h"
#include "Message.h"
#include "DeltaCompress.h"
#include <iostream>

using namespace DE;

void Level::Setup(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_)
{
    _lib = lib;

    tileset = tileset_;
}

void Level::Loaded()
{
}

void Level::CreateTilemap()
{
    _tilemaps.CreateMesh(_lib, 10, 10, 16, 9);
    _tilemaps.SetTileSet(tileset);
}

void Level::Render(IslanderRenderable* renderables, int* cur_index, const int tilemappixelShader, const int tilemapvertexShader)
{
    _tilemaps.Update();

    const int numlayers = _tilemaps.NumLayers();
    for (int i = 0; i < numlayers; i++)
    {
        auto& layer = _tilemaps.GetLayer(i);

        if (layer.Visible())
        {
            IslanderRenderable* renderable = &renderables[*cur_index];
            std::memset(renderable, 0, sizeof(IslanderRenderable));

            renderable->mesh.pixelShader = tilemappixelShader;
            renderable->mesh.vertexShader = tilemapvertexShader;
            renderable->mesh.geometryShader = -1;
            renderable->mesh.polydata = _tilemaps.GetMesh();
            renderable->mesh.parentEntity = -1;
            renderable->mesh.material.slot_data[0] = _tilemaps.GetTileset()->GetTexture();
            renderable->mesh.material.slot_flags[0] = ISLANDER_RENDERABLE_MATERIAL_SLOT_CUSTOM;

            Tilemap_ConstantBuffer* buffer = layer.GetConstantBuffer();
            renderable->mesh.constantBufferData = buffer;
            renderable->mesh.constantBufferDataSize = sizeof(Tilemap_ConstantBuffer);

            (*cur_index)++;
        }
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

