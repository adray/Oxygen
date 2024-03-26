#include "Tilemap.h"
#include "Message.h"

#include <iostream>

using namespace DE;

Tilemap::Tilemap()
    : Tilemap(-1)
{
}

Tilemap::Tilemap(int id)
    :
    _id(id),
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
    _mesh(nullptr),
    _tiles(nullptr)
{
    _constant = new ConstantBuffer();
}

Tilemap::Tilemap(const Tilemap& tilemap)
    :
    Tilemap(tilemap.ID())
{
    std::memcpy(_constant->tileUVs, tilemap._constant->tileUVs, sizeof(_constant->tileUVs));
}

Tilemap::~Tilemap()
{
    delete[] _vertexData;
    delete[] _indexData;
    delete _constant;
    delete[] _tiles;

    _vertexData = nullptr;
    _indexData = nullptr;
    _constant = nullptr;
    _tiles = nullptr;
}

void Tilemap::Update()
{
    int tile = 0;
    for (int i = 0; i < _viewheight; i++)
    {
        for (int j = 0; j < _viewwidth; j++)
        {
            int tileID = _tiles[(i + _scrollY) * _width + (j + _scrollX)];

            Islander::component_texture texture;
            _tileset->GetTile(tileID, texture);

            _constant->tileUVs[tile++] = texture.px;
            _constant->tileUVs[tile++] = texture.py;
            _constant->tileUVs[tile++] = texture.sx;
            _constant->tileUVs[tile++] = texture.sy;
        }
    }
}

void Tilemap::Load(const int width, const int height)
{
    _width = width;
    _height = height;

    const int numTiles = _width * _height;

    _tiles = new int[numTiles];
    std::memset(_tiles, 0, sizeof(int) * numTiles);
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

int Tilemap::HitTest(int x, int y) const
{
    const int displayWidth = _viewwidth * _tileWidth * 4; /*640.0f*/
    const int displayHeight = _viewheight * _tileHeight * 4;/*360*/

    if (x >= 0 && x <= displayWidth && y >= 0 && y <= displayHeight)
    {
        const int px = _viewwidth * (x / float(displayWidth)) + _scrollX;
        const int py = _viewheight * (y / float(displayHeight)) + _scrollY;

        return px + py * _width;
    }

    return -1;
}

void Tilemap::Set(int cell, int tile)
{
    _tiles[cell] = tile;
}

void Tilemap::SetScrollPos(int x, int y)
{
    _scrollX = std::max(0, std::min(_width-_viewwidth, x));
    _scrollY = std::max(0, std::min(_height-_viewheight, y));
}

bool Tilemap::GetTileBounds(int tile, float* px, float* py, float* sx, float* sy)
{
    int x = tile % _width;
    int y = tile / float(_width);

    x -= _scrollX;
    y -= _scrollY;
    if (x >= 0 && y >= 0 &&
        x < _viewwidth && y < _viewheight)
    {
        *px = x * _tileWidth * 4;
        *py = y * _tileHeight * 4;
        *sx = _tileWidth * 4;
        *sy = _tileHeight * 4;
        return true;
    }

    return false;
}

void Tilemap::Serialize(Oxygen::Message& msg)
{
    const int tileSize = _width * _height * sizeof(int);

    msg.WriteInt32(_width);
    msg.WriteInt32(_height);
    msg.WriteBytes(tileSize, (unsigned char*)_tiles);
}

void Tilemap::Deserialize(Oxygen::Message& msg)
{
    _width = msg.ReadInt32();
    _height = msg.ReadInt32();
    int numBytes = msg.ReadInt32();
    _tiles = new int[numBytes / sizeof(int)];
    msg.ReadBytes(numBytes, (unsigned char*)_tiles);
}

void Tilemap::Clear()
{
    _id = -1;
    _width = 0;
    _height = 0;
    _scrollX = 0;
    _scrollY = 0;
    delete[] _tiles;
    _tiles = nullptr;
}
