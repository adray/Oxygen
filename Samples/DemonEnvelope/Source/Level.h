#pragma once
#include "Tilemap.h"
#include "Scripting.h"
#include <vector>
#include <unordered_map>

namespace DE
{
    class Level
    {
    public:
        void Setup(ISLANDER_POLYGON_LIBRARY lib, std::shared_ptr<Tileset> tileset_);
        void Loaded();
        void Render(IslanderRenderable* renderables, int* cur_index, const int tilemappixelShader, const int tilemapvertexShader);
        void CreateTilemap();

        inline std::shared_ptr<Tileset> TileSet() { return tileset; }
        bool TileMapHitTest(int x, int y) const;
        Tilemap& GetTilemap() { return _tilemaps; }
        void Reset();

    private:
        Tilemap _tilemaps;
        Scripting _scripting;
        std::shared_ptr<Tileset> tileset;
        ISLANDER_POLYGON_LIBRARY _lib;
    };
}
