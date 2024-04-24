#include "SpriteBatch.h"

using namespace DE;

//======================
//class Sprite
//======================

SpriteBatch_Sprite::SpriteBatch_Sprite(float pos[3], float uv[4])
{
    std::memcpy(_pos, pos, sizeof(_pos));
    std::memcpy(_uv, uv, sizeof(_uv));
}

void SpriteBatch_Sprite::GetPos(float pos[3])
{
    std::memcpy(pos, _pos, sizeof(_pos));
}

void SpriteBatch_Sprite::GetUV(float uv[4])
{
    std::memcpy(uv, _uv, sizeof(_uv));
}

//======================
// class SpriteBatch
//======================

SpriteBatch::SpriteBatch()
    :
    _lib(nullptr),
    _mesh(nullptr),
    _indexData(nullptr),
    _vertexData(nullptr),
    _constantData(nullptr)
{
}

void SpriteBatch::Initialize(ISLANDER_POLYGON_LIBRARY lib)
{
    _lib = lib;
    _constantData = new SpriteBatch_ConstantBuffer();
    std::memset(_constantData->tilesColour, 0, sizeof(_constantData->tilesColour));
    std::memset(_constantData->tilesPos, 0, sizeof(_constantData->tilesPos));
    std::memset(_constantData->tilesUV, 0, sizeof(_constantData->tilesUV));

    const int numTiles = 64;
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
        AddTile(vertexPos, indexPos, vertexID);
    }

    _mesh = IslanderAddPolyMeshData(_lib, (float*)_vertexData, _indexData, numVerts, indexCount, stride, 0x4 /* Copy the mesh data */ | 0x2 /* Generate AABB */);
}

void SpriteBatch::Reset()
{
    _sprites.clear();
}

void SpriteBatch::AddSprite(const SpriteBatch_Sprite& sprite)
{
    _sprites.push_back(sprite);
}

void SpriteBatch::AddTile(int& vertexPos, int& indexPos, int& vertexID)
{
    // Top-Left
    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Top-Right
    ((float*)_vertexData)[vertexPos++] = 16.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 1.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Bottom-Left
    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 9.0f;
    ((float*)_vertexData)[vertexPos++] = 0.0f;

    ((float*)_vertexData)[vertexPos++] = 0.0f;
    ((float*)_vertexData)[vertexPos++] = 1.0f;

    ((int*)_vertexData)[vertexPos++] = vertexID / 4;

    // Bottom-Right
    ((float*)_vertexData)[vertexPos++] = 16.0f;
    ((float*)_vertexData)[vertexPos++] = 9.0f;
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

void SpriteBatch::Update()
{
    for (int i = 0; i < _sprites.size(); i++)
    {
        float pos[3];
        float uv[4];

        _sprites[i].GetPos(pos);
        _sprites[i].GetUV(uv);

        _constantData->tilesPos[i * 4] = pos[0];
        _constantData->tilesPos[i * 4 + 1] = pos[1];
        _constantData->tilesPos[i * 4 + 2] = pos[2];

        _constantData->tilesUV[i * 4] = uv[0];
        _constantData->tilesUV[i * 4 + 1] = uv[1];
        _constantData->tilesUV[i * 4 + 2] = uv[2];
        _constantData->tilesUV[i * 4 + 3] = uv[3];

        _constantData->tilesColour[i * 4] = 1.0f;
        _constantData->tilesColour[i * 4 + 1] = 1.0f;
        _constantData->tilesColour[i * 4 + 2] = 1.0f;
        _constantData->tilesColour[i * 4 + 3] = 1.0f;
    }
}
