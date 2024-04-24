#include "Common.fx"

CLAMP_SAMPLER;
DIFFUSE;
SAMPLE_DIFFUSE_CLAMP;

cbuffer ConstantData : register(b0)
{
	COMMON_PARAMS;
    float screenWidth;
    float screenHeight;
    float2 padding;
};

cbuffer ConstantData : register(b1)
{
    float4 tilesUV[64];
    float4 tilesColour[64];
    float4 tilesPos[64];
};

float4 SpriteBatchVertex(in float3 pos : POSITION, in float2 UVOffset : TEXTURE, in int tileID : TILE_ID, out float2 outUV : TEXTURE, out float4 outColour : COLOR) : SV_POSITION
{
    float scaleFactorX = 4;
    float scaleFactorY = 4;
    
    float2 uvScale = tilesUV[tileID].zw;
    float2 tilePos = tilesPos[tileID].xy;
    
    outUV = tilesUV[tileID].xy + UVOffset * uvScale;
    outColour = tilesColour[tileID];
	
    return float4((pos.x + tilePos.x) * scaleFactorX * 2 / screenWidth, -(pos.y + tilePos.y) * scaleFactorY * 2 / screenHeight, pos.z, 1.0f);
}

float4 SpriteBatchPixel(in float4 pos : SV_POSITION, in float2 uv : TEXTURE, in float4 inColour : COLOR) : SV_Target
{
    float4 color = SampleDiffuseClamp(uv);
	
	//return float4(1.f, 0.f, 0.f, 1.0f);
    return color * inColour;
}
