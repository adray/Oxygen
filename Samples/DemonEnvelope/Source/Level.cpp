#include "Level.h"
#include "Message.h"
#include "DeltaCompress.h"
#include <iostream>

using namespace DE;

//===================
// Level_Entity class
//===================

void Level_Entity::SetPos(int px, int py)
{
    _px = px;
    _py = py;
}

//==================
// Level class
//==================

void Level::Setup(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_)
{
    _lib = lib;

    tileset = tileset_;

    _sprites.Initialize(lib);
}

void Level::Loaded()
{
}

void Level::CreateTilemap()
{
    _tilemaps.CreateMesh(_lib, 10, 10, 16, 9);
    _tilemaps.SetTileSet(tileset);
}

void Level::Render(IslanderRenderable* renderables, int* cur_index,
    const int tilemappixelShader, const int tilemapvertexShader,
    const int spriteBatchpixelShader, const int spriteBatchvertexShader)
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

    _sprites.Reset();

    for (int i = 0; i < _numEntities; i++)
    {
        auto& entity = _entities[i];
        if (entity.IsActive())
        {
            float pos[3] = {
                (entity.X() - _tilemaps.ScrollX()) * 16,
                (entity.Y() - _tilemaps.ScrollY()) * 9,
                0.0f
            };
            float uv[4] = {
                0.0839843750f,
                0.00195312500f,
                0.0156250000f,
                0.00878906250f
            };

            _sprites.AddSprite(SpriteBatch_Sprite(pos, uv));
        }
    }

    _sprites.Update();

    IslanderRenderable* renderable = &renderables[*cur_index];
    std::memset(renderable, 0, sizeof(IslanderRenderable));

    renderable->mesh.pixelShader = spriteBatchpixelShader;
    renderable->mesh.vertexShader = spriteBatchvertexShader;
    renderable->mesh.geometryShader = -1;
    renderable->mesh.polydata = _sprites.GetMesh();
    renderable->mesh.parentEntity = -1;
    renderable->mesh.material.slot_data[0] = tileset->GetTexture();
    renderable->mesh.material.slot_flags[0] = ISLANDER_RENDERABLE_MATERIAL_SLOT_CUSTOM;

    SpriteBatch_ConstantBuffer* buffer = _sprites.ConstantData();
    renderable->mesh.constantBufferData = buffer;
    renderable->mesh.constantBufferDataSize = sizeof(SpriteBatch_ConstantBuffer);

    (*cur_index)++;
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

void Level::ClearEntities()
{
    _numEntities = 0;
}

int Level::AddEntity()
{
    int id = -1;
    for (int i = 0; i < _numEntities; i++)
    {
        if (!_entities[i].IsActive())
        {
            id = i;
            break;
        }
    }

    if (id == -1 && _numEntities < _entities.size())
    {
        id = _numEntities;
        _numEntities++;
    }

    if (id != -1)
    {
        _entities[id].SetActive(true);
    }

    return id;
}

void Level::RemoveEntity(int entity)
{
    _entities[entity].SetActive(false);
}

void Level::SetEntityPos(int entity, int px, int py)
{
    Level_Entity& ent = _entities[entity];
    if (px >= 0 && px < _tilemaps.Width() &&
        py >= 0 && py < _tilemaps.Height())
    {
        auto& mask = _tilemaps.GetCollisionMask();
        if (!mask.Get(px + py * _tilemaps.Width()))
        {
            ent.SetPos(px, py);
        }
    }
}

void Level::GetEntityPos(int entity, int* px, int* py)
{
    Level_Entity& ent = _entities[entity];
    *px = ent.X();
    *py = ent.Y();
}
