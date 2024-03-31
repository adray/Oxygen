#pragma once
#include <API.h>

namespace DE
{
    class ConfigReader;

    struct Tileset_Tile
    {
        Islander::component_texture _texture;
        int _layer;
        int _id;
        bool _walkable;
    };

    class Tileset
    {
    public:
        Tileset();
        void Load(ISLANDER_DEVICE device, ConfigReader& cfg);
        inline int GetTexture() const { return _texture; }
        void GetTile(int id, Tileset_Tile& tile) const;
        inline int NumTiles() const { return (int)tiles.size(); }
    private:
        int _texture;
        std::vector<Tileset_Tile> tiles;
    };
}
