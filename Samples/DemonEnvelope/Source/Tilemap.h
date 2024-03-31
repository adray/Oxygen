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
    struct Tilemap_ConstantBuffer
    {
        float tileUVs[400];
        float tileColour[400];
    };

    class Tilemap_Mask
    {
    public:
        Tilemap_Mask();

        void Load(const int width, const int height);
        void Set(int cell, int tile);
        bool Get(int cell) const;
        inline int Width() const { return _width; }
        inline int Height() const { return _height; }

        void Serialize(Oxygen::Message& msg) const;

        void Deserialize(Oxygen::Message& msg);

        inline int ID() const { return _id; }
        inline void SetID(int id) { _id = id; }

        inline void SetVersion(int version) { _version = version; }

        inline int Version() const { return _version; }

    private:
        unsigned char* _mask;
        int _numBytes;
        int _width;
        int _height;
        int _version;
        int _id;
    };

    class Tilemap_Layer
    {
    public:
        Tilemap_Layer();

        void SetTileSet(std::shared_ptr<Tileset>& tileset) { _tileset = tileset; }
        std::shared_ptr<Tileset> GetTileset() { return _tileset; }
        
        void Load(const int layer, const int width, const int height);

        void Set(int cell, int tile);

        void Update(int scrollX, int scrollY, int viewwidth, int viewheight);

        void Serialize(Oxygen::Message& msg) const;

        void Deserialize(Oxygen::Message& msg);

        inline int ID() const { return _id; }

        inline void SetID(int id) { _id = id; }

        inline void SetVersion(int version) { _version = version; }

        inline int Version() const { return _version; }

        inline Tilemap_ConstantBuffer* GetConstantBuffer() const { return _constant; }

        inline int Layer() const { return _layer; }

        inline bool Visible() const { return _visible; }

        inline void SetVisible(bool visible) { _visible = visible; }

        ~Tilemap_Layer();

    private:
        int* _tiles;
        int _width;
        int _height;
        int _id;
        int _layer;
        int _version;
        bool _visible;
        std::shared_ptr<Tileset> _tileset;
        Tilemap_ConstantBuffer* _constant;
    };

    class Tilemap
    {
    public:

        Tilemap();
        Tilemap(const Tilemap& tilemap);

        void CreateLayers(int numLayers);

        void SetTileSet(std::shared_ptr<Tileset>& tileset) { _tileset = tileset; }
        std::shared_ptr<Tileset> GetTileset() { return _tileset; }

        void Load(const int width, const int height);

        void CreateMesh(ISLANDER_POLYGON_LIBRARY lib, const int viewwidth, const int viewheight, const int tileWidth, const int tileHeight);

        void Update();

        inline ISLANDER_POLYGON_DATA GetMesh() { return _mesh; }

        void Set(int layer, int cell, int tile);

        int HitTest(int x, int y) const;

        void SetScrollPos(int x, int y);

        inline int ScrollX() const { return _scrollX; }

        inline int ScrollY() const { return _scrollY; }

        inline int NumTiles() const { return _width * _height; }

        inline size_t NumLayers() const { return _layers.size(); }

        inline Tilemap_Layer& GetLayer(int layer) { return _layers[layer]; }

        bool GetTileBounds(int tile, float* px, float* py, float* sx, float* sy);

        void Clear();

        ~Tilemap();

    private:
        void AddTile(int& vertexPos, int& indexPos, int& vertexID, int x, int y);

        std::vector<Tilemap_Layer> _layers;
        std::unique_ptr<Tilemap_Mask> _collider;
        std::shared_ptr<Tileset> _tileset;
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
