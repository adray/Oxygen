#pragma once
#include <vector>
#include "API.h"

namespace DE
{
    class SpriteBatch_Sprite
    {
    public:
        SpriteBatch_Sprite(float pos[3], float uv[4]);
        void GetPos(float pos[3]);
        void GetUV(float uv[4]);

    private:
        float _pos[3];
        float _uv[4];
    };

    struct SpriteBatch_ConstantBuffer
    {
        float tilesUV[64 * 4];
        float tilesColour[64 * 4];
        float tilesPos[64 * 4];
    };

    class SpriteBatch
    {
    public:
        SpriteBatch();
        void Initialize(ISLANDER_POLYGON_LIBRARY lib);
        void Reset();
        void AddSprite(const SpriteBatch_Sprite& sprite);
        void Update();

        inline ISLANDER_POLYGON_DATA GetMesh() { return _mesh; }
        inline SpriteBatch_ConstantBuffer* ConstantData() const { return _constantData; }

    private:
        void AddTile(int& vertexPos, int& indexPos, int& vertexID);

        SpriteBatch_ConstantBuffer* _constantData;
        std::vector<SpriteBatch_Sprite> _sprites;
        ISLANDER_POLYGON_LIBRARY _lib;
        ISLANDER_POLYGON_DATA _mesh;
        unsigned char* _vertexData;
        int* _indexData;
    };
}
