#include "Common.fx"

CLAMP_SAMPLER;
DIFFUSE;
SAMPLE_DIFFUSE_CLAMP;

cbuffer ConstantData : register (b0)
{
	COMMON_PARAMS;
    float screenWidth;
    float screenHeight;
    float2 padding;
};

cbuffer ConstantData : register(b1)
{
    float4 tiles[100];
};

float4 TilemapVertex(in float3 pos : POSITION, in float2 UVOffset : TEXTURE, in int tileID : TILE_ID, out float2 outUV : TEXTURE) : SV_POSITION
{
    float scaleFactorX = 4;
    float scaleFactorY = 4;
    
    float2 uvScale = tiles[tileID].zw; //float2(16.0f / 1024, 12.0f / 1024);
    
    outUV = tiles[tileID].xy + UVOffset * uvScale;
	
    return float4(pos.x * scaleFactorX * 2 / screenWidth, -pos.y * scaleFactorY * 2 / screenHeight, pos.z, 1.0f);
}

float4 TilemapPixel(in float4 pos : SV_POSITION, in float2 uv : TEXTURE) : SV_Target
{
    float4 color = SampleDiffuseClamp(uv);
	
	//return float4(1.f, 0.f, 0.f, 1.0f);
    return color;
}
