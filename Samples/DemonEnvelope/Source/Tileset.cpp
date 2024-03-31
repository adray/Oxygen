#include "Tileset.h"
#include "ConfigReader.h"

using namespace DE;

Tileset::Tileset()
    : 
    _texture(0)
{
}

void Tileset::Load(ISLANDER_DEVICE device, ConfigReader& cfg)
{
    while (cfg.ReadNextRow())
    {
        const int id = std::strtol(cfg.Get("id").c_str(), 0, 10);
        const std::string name = cfg.Get("name");
        const int layer = std::strtol(cfg.Get("layer").c_str(), 0, 10);

        Tileset_Tile tile = {};
        IslanderFindMaterialTexture(device, name.c_str(), &tile._texture);
        tile._layer = layer;
        tile._id = id;
        tiles.push_back(tile);

        if (tiles.size() == 1)
        {
            _texture = tile._texture.index;
        }
    }
}


void Tileset::GetTile(int id, Tileset_Tile& texture) const
{
    texture = tiles[id];
}
