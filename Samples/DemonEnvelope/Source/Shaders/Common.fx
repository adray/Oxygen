

#define COMMON_PARAMS \
    float4x4 world; \
    float4x4 proj; \
    float4x4 view


#define DIFFUSE Texture2D diffuse : register(t0)
#define CLAMP_SAMPLER SamplerState clampSampler : register(s0)

#define SAMPLE_DIFFUSE_CLAMP \
float4 SampleDiffuseClamp(float2 uv) \
{ \
    return diffuse.Sample(clampSampler, uv); \
}
