#pragma once
#include "Tilemap.h"
#include "Scripting.h"
#include "SpriteBatch.h"
#include <vector>
#include <unordered_map>
#include <array>

namespace DE
{
    class Level_Entity
    {
    public:
        void SetPos(int px, int py);
        inline int X() const { return _px; }
        inline int Y() const { return _py; }
        inline bool IsActive() const { return _active; }
        inline void SetActive(bool active) { _active = active; }
        inline void SetTileId(int tile) { _tileId = tile; }
        inline int TileId() const { return _tileId; }

    private:
        bool _active;
        int _px;
        int _py;
        int _tileId;
    };

    class Level
    {
    public:
        void Setup(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_);
        void Loaded();
        void Render(IslanderRenderable* renderables, int* cur_index,
            const int tilemappixelShader, const int tilemapvertexShader,
            const int spriteBatchpixelShader, const int spriteBatchvertexShader);
        void CreateTilemap();

        inline std::shared_ptr<Tileset> TileSet() { return tileset; }
        bool TileMapHitTest(int x, int y) const;
        Tilemap& GetTilemap() { return _tilemaps; }
        void Reset();

        void ClearEntities();
        int AddEntity();
        void RemoveEntity(int entity);
        void SetEntityPos(int entity, int px, int py);
        void GetEntityPos(int entity, int* px, int* py);

    private:
        Tilemap _tilemaps;
        Scripting _scripting;
        std::shared_ptr<Tileset> tileset;
        ISLANDER_POLYGON_LIBRARY _lib;
        SpriteBatch _sprites;
        std::array<Level_Entity, 128> _entities;
        int _numEntities;
    };
}
