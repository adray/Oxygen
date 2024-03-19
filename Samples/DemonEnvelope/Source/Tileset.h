#pragma once
#include <API.h>

namespace DE
{
    class Tileset
    {
    public:
        Tileset();
        void Load(ISLANDER_DEVICE device);
        inline int GetTexture() const { return _texture; }
        void GetTile(int id, Islander::component_texture& texture) const;
        inline int NumTiles() const { return (int)tiles.size(); }
    private:
        int _texture;
        std::vector<Islander::component_texture> tiles;
    };
}
