#pragma once
#include "API.h"
#include "Tileset.h"
#include <memory>

namespace Oxygen
{
    class Message;
}

namespace DE
{
    class Tilemap
    {   
    public:
        struct ConstantBuffer
        {
            float tileUVs[400];
        };

        Tilemap();
        Tilemap(int id);
        Tilemap(const Tilemap& tilemap);

        void SetTileSet(std::shared_ptr<Tileset>& tileset) { _tileset = tileset; }
        std::shared_ptr<Tileset> GetTileset() { return _tileset; }

        void Load(const int width, const int height);

        void CreateMesh(ISLANDER_POLYGON_LIBRARY lib, const int viewwidth, const int viewheight, const int tileWidth, const int tileHeight);

        void Update();

        inline ISLANDER_POLYGON_DATA GetMesh() { return _mesh; }

        inline Tilemap::ConstantBuffer* GetConstantBuffer() { return _constant; }

        int HitTest(int x, int y) const;

        void Set(int cell, int tile);

        void SetScrollPos(int x, int y);

        void Serialize(Oxygen::Message& msg);

        void Deserialize(Oxygen::Message& msg);

        inline int ID() const { return _id; }

        inline void SetID(int id) { _id = id; }

        inline int ScrollX() const { return _scrollX; }

        inline int ScrollY() const { return _scrollY; }

        inline int NumTiles() const { return _width * _height; }

        void Clear();

        ~Tilemap();

    private:
        void AddTile(int& vertexPos, int& indexPos, int& vertexID, int x, int y);

        int _id;
        std::shared_ptr<Tileset> _tileset;
        ConstantBuffer* _constant;
        int* _tiles;
        unsigned char* _vertexData;
        int* _indexData;
        int _width;
        int _height;
        int _viewwidth;
        int _viewheight;
        int _tileWidth;
        int _tileHeight;
        int _scrollX;
        int _scrollY;
        ISLANDER_POLYGON_DATA _mesh;
    };
}
