#include "Tilemap.h"
#include "Message.h"

#include <iostream>

using namespace DE;

//======================
// Tilemap_Collider
//======================

Tilemap_Mask::Tilemap_Mask()
    :
    _width(0),
    _height(0),
    _numBytes(0),
    _mask(nullptr),
    _id(0),
    _version(0)
{
}

void Tilemap_Mask::Load(const int width, const int height)
{
    _width = width;
    _height = height;

    const int numTiles = _width * _height;
    _numBytes = (numTiles / 8) * 8 + (numTiles % 8 > 0 ? 1 : 0);
    
    _mask = new unsigned char[_numBytes];
    std::memset(_mask, 0, _numBytes);
}

bool Tilemap_Mask::Get(int cell) const
{
    if (!_mask) { return false; }

    unsigned char value = _mask[cell / 8];
    return ((value >> (cell % 8)) & 0x1) == 1;
}

void Tilemap_Mask::Set(int cell, bool value)
{
    if (_mask) {
        _mask[cell / 8] |= (value ? 0x1 : 0x0) << (cell % 8);
    }
}

void Tilemap_Mask::Serialize(Oxygen::Message& msg) const
{
    msg.WriteInt32(_width);
    msg.WriteInt32(_height);
    msg.WriteBytes(_numBytes, _mask);
}

void Tilemap_Mask::Deserialize(Oxygen::Message& msg)
{
    delete[] _mask;

    _width = msg.ReadInt32();
    _height = msg.ReadInt32();
    int numBytes = msg.ReadInt32();
    _mask = new unsigned char[numBytes];
    msg.ReadBytes(numBytes, _mask);
}

//======================
// Tilemap_Layer
//======================

Tilemap_Layer::Tilemap_Layer()
    :
    _width(0),
    _height(0),
    _layer(0),
    _tiles(nullptr),
    _id(0),
    _version(0),
    _visible(true)
{
    _constant = new Tilemap_ConstantBuffer();
}

void Tilemap_Layer::Load(const int layer, const int width, const int height)
{
    _layer = layer;
    _width = width;
    _height = height;

    const int numTiles = _width * _height;

    _tiles = new int[numTiles];
    std::memset(_tiles, 0, sizeof(int) * numTiles);
}

void Tilemap_Layer::Set(int cell, int tile)
{
    _tiles[cell] = tile;
}

void Tilemap_Layer::Update(int scrollX, int scrollY, int viewwidth, int viewheight)
{
    int tileIndex = 0;
    for (int i = 0; i < viewheight; i++)
    {
        for (int j = 0; j < viewwidth; j++)
        {
            const int tileID = _tiles[(i + scrollY) * _width + (j + scrollX)];

            Tileset_Tile tile;
            _tileset->GetTile(tileID, tile);

            const Islander::component_texture& texture = tile._texture;

            _constant->tileUVs[tileIndex] = texture.px;
            _constant->tileUVs[tileIndex +1] = texture.py;
            _constant->tileUVs[tileIndex +2] = texture.sx;
            _constant->tileUVs[tileIndex +3] = texture.sy;

            _constant->tileColour[tileIndex] = _layer == tile._layer ? 1.0f : 0.0f;
            _constant->tileColour[tileIndex +1] = _layer == tile._layer ? 1.0f : 0.0f;
            _constant->tileColour[tileIndex +2] = _layer == tile._layer ? 1.0f : 0.0f;
            _constant->tileColour[tileIndex +3] = _layer == tile._layer ? 1.0f : 0.0f;

            tileIndex += 4;
        }
    }
}

void Tilemap_Layer::Serialize(Oxygen::Message& msg) const
{
    const int tileSize = _width * _height * sizeof(int);

    msg.WriteInt32(_width);
    msg.WriteInt32(_height);
    msg.WriteBytes(tileSize, (unsigned char*)_tiles);
}

void Tilemap_Layer::Deserialize(Oxygen::Message& msg)
{
    delete[] _tiles;

    _width = msg.ReadInt32();
    _height = msg.ReadInt32();
    int numBytes = msg.ReadInt32();
    _tiles = new int[numBytes / sizeof(int)];
    msg.ReadBytes(numBytes, (unsigned char*)_tiles);
}

Tilemap_Layer::~Tilemap_Layer()
{
    delete[] _tiles;
    delete _constant;
    _tiles = nullptr;
    _constant = nullptr;
}

//======================
// Tilemap
//======================

Tilemap::Tilemap()
    :
    _vertexData(nullptr),
    _indexData(nullptr),
    _viewwidth(0),
    _viewheight(0),
    _tileWidth(0),
    _tileHeight(0),
    _width(0),
    _height(0),
    _scrollX(0),
    _scrollY(0),
    _mesh(nullptr)
{
}

Tilemap::Tilemap(const Tilemap& tilemap)
    :
    Tilemap()
{
    //std::memcpy(_constant->tileUVs, tilemap._constant->tileUVs, sizeof(_constant->tileUVs));
}

Tilemap::~Tilemap()
{
    delete[] _vertexData;
    delete[] _indexData;

    _vertexData = nullptr;
    _indexData = nullptr;
}

void Tilemap::CreateLayers(int numLayers)
{
    _layers.resize(numLayers);

    int index = 0;
    for (auto& layer : _layers)
    {
        layer.Load(index++, _width, _height);
        layer.SetTileSet(_tileset);
    }

    _collider.Load(_width, _height);
}

void Tilemap::Update()
{
    for (auto& layer : _layers)
    {
        layer.Update(_scrollX, _scrollY, _viewwidth, _viewheight);
    }
}

void Tilemap::Load(const int width, const int height)
{
    _width = width;
    _height = height;
}

void Tilemap::AddTile(int& vertexPos, int& indexPos, int& vertexID, int x, int y)
{
    // Top-Left
    ((float*)_vertexData)[vertexPos++] = float(x * _tileWidth);
    ((float*)_vertexData)[vertexPos++] = float(y * _tileHeight);
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Top-Right
    ((float*)_vertexData)[vertexPos++] = float((x+1) * _tileWidth);
    ((float*)_vertexData)[vertexPos++] = float(y * _tileHeight);
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 1.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Bottom-Left
    ((float*)_vertexData)[vertexPos++] = float(x * _tileWidth);
    ((float*)_vertexData)[vertexPos++] = float((y+1) * _tileHeight);
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 1.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Bottom-Right
    ((float*)_vertexData)[vertexPos++] = float((x+1) * _tileWidth);
    ((float*)_vertexData)[vertexPos++] = float((y+1) * _tileHeight);
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 1.0f;
    ((float*)_vertexData)[vertexPos++] = 1.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Index data
    _indexData[indexPos++] = vertexID;
    _indexData[indexPos++] = vertexID + 1;
    _indexData[indexPos++] = vertexID + 3;

    _indexData[indexPos++] = vertexID + 3;
    _indexData[indexPos++] = vertexID + 2;
    _indexData[indexPos++] = vertexID;

    vertexID += 4;
}

void Tilemap::CreateMesh(ISLANDER_POLYGON_LIBRARY lib, const int viewwidth, const int viewheight, const int tileWidth, const int tileHeight)
{
    _viewwidth = viewwidth;
    _viewheight = viewheight;

    _tileWidth = tileWidth;
    _tileHeight = tileHeight;

    if (_vertexData)
    {
        delete[] _vertexData;
    }

    if (_indexData)
    {
        delete[] _indexData;
    }

    const int numTiles = viewwidth * viewheight;
    const int numVerts = numTiles * 4;
    const int stride = 5 * sizeof(float) + sizeof(int);
    const int size = stride * numVerts;
    _vertexData = new unsigned char[size];

    const int indexCount = numTiles * 6;
    _indexData = new int[indexCount];

    int vertexPos = 0;
    int indexPos = 0;
    int vertexID = 0;
    for (int i = 0; i < numTiles; i++)
    {
        AddTile(vertexPos, indexPos, vertexID, i % viewwidth, i / viewwidth);
    }

    _mesh = IslanderAddPolyMeshData(lib, (float*)_vertexData, _indexData, numVerts, indexCount, stride, 0x4 /* Copy the mesh data */ | 0x2 /* Generate AABB */);
}

void Tilemap::Set(int layer, int cell, int tile)
{
    _layers[layer].Set(cell, tile);

    if (layer == _layers.size() - 1)
    {
        Tileset_Tile tileset_tile;
        _tileset->GetTile(tile, tileset_tile);
        _collider.Set(cell, tileset_tile._walkable ? 0 : 1);
    }
}

int Tilemap::HitTest(int x, int y) const
{
    const int displayWidth = _viewwidth * _tileWidth * 4; /*640.0f*/
    const int displayHeight = _viewheight * _tileHeight * 4;/*360*/

    if (x >= 0 && x <= displayWidth && y >= 0 && y <= displayHeight)
    {
        const int px = int(_viewwidth * (x / float(displayWidth)) + _scrollX);
        const int py = int(_viewheight * (y / float(displayHeight)) + _scrollY);

        return px + py * _width;
    }

    return -1;
}

void Tilemap::SetScrollPos(int x, int y)
{
    _scrollX = std::max(0, std::min(_width-_viewwidth, x));
    _scrollY = std::max(0, std::min(_height-_viewheight, y));
}

bool Tilemap::GetTileBounds(int tile, float* px, float* py, float* sx, float* sy)
{
    int x = tile % _width;
    int y = int(tile / float(_width));

    x -= _scrollX;
    y -= _scrollY;
    if (x >= 0 && y >= 0 &&
        x < _viewwidth && y < _viewheight)
    {
        *px = float(x * _tileWidth * 4);
        *py = float(y * _tileHeight * 4);
        *sx = float(_tileWidth * 4);
        *sy = float(_tileHeight * 4);
        return true;
    }

    return false;
}

void Tilemap::Clear()
{
    _width = 0;
    _height = 0;
    _scrollX = 0;
    _scrollY = 0;
    _layers.clear();
}
