#include "Tileset.h"

using namespace DE;

Tileset::Tileset()
    : 
    _texture(0)
{
}

void Tileset::Load(ISLANDER_DEVICE device)
{
    std::string strs[] = {
        "tile01",
        "tile02",
        "tile03"
    };

    for (int i = 0; i < 3; i++)
    {
        Islander::component_texture texture;
        IslanderFindMaterialTexture(device, strs[i].c_str(), &texture);
        tiles.push_back(texture);

        if (i == 0)
        {
            _texture = texture.index;
        }
    }
}


void Tileset::GetTile(int id, Islander::component_texture& texture) const
{
    texture = tiles[id];
}
