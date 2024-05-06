#include "Common.fx"

CLAMP_SAMPLER;
DIFFUSE;
SAMPLE_DIFFUSE_CLAMP;

cbuffer Constants : register (b0)
{

	float4 pos;
	float4 fontcolor;
	float clipX;
	float clipY;
	float clipZ;
	float clipW;
	float opacity;
    float3 pad;
};

float4 CrimsonFontVertex(float4 position : POSITION, float index : TEXCOORD0, out float3 tex : TEXCOORD0/*,
	out float4 clipdist : SV_ClipDistance0*/) : SV_POSITION
{
	tex = float3(position.zw, index);
	
	float4 outpos = float4(position.xy+pos.xy, pos.z, 1);
	
	/*clipdist = float4(
		clipX-outpos.x,
		outpos.x-clipY,
		outpos.y-clipZ,
		clipW-outpos.y);*/
	
    return outpos;
}

void CrimsonFontPixel(in float4 pos : SV_POSITION, in float3 tex : TEXCOORD0,
	out float4 outcolor : SV_Target)
{    
    int strike = (((int)tex.z) & 0x2);
    if (strike == 2)
    {
        outcolor.rgb = fontcolor;
        outcolor.a = 1;
    }
    else
    {
        float bold = ((int)tex.z) & 0x1;
        float link = (((int)tex.z) & 0x4) / 4;
        
        //float4 color = fontmap.Sample(fontSampler, tex.xy).rrrr;
        float4 color = SampleDiffuseClamp(tex.xy).rrrr;
        
        float4 col = float4(1,0,0,fontcolor.a) * bold + (1-bold) * (1-link) * fontcolor + (1-bold) * link * float4(0,0,1,fontcolor.a);  
        //fontcolor = (1-strike) * fontcolor + strike * float4(1,0,0,1);
        
        outcolor.rgb=col.rgb*color.a*/*opacity**/col.a;
        outcolor.a=color.a*col.a/**opacity*/;
    }
    
    // debug
    //float bold = ((int)tex.z) & 0x1;
    //outcolor.rgba = float4(1,1,1,1) * (1-bold) + float4(1,0,0,0/*fontcolor.a*/) * bold;
}





