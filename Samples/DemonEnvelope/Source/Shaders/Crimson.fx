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


float4 CrimsonLineVertex(in float3 pos : POSITION, in float4 color : COLOR, out float4 outColor : COLOR) : SV_POSITION
{
    outColor = color;
    return float4(pos, 1.0f);
}

float4 CrimsonLinePixel(in float4 pos : SV_POSITION, in float4 color : COLOR) : SV_Target
{
    //float4 color = SampleDiffuseClamp(uv);
	
	//return float4(1.f, 0.f, 0.f, 1.0f);
    return color;
}
