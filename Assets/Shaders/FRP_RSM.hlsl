#ifndef __FRP_RSM__
#define __FRP_RSM__

#include "./UnityHLSL.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    TEXTURE2D(_MainTex);
    float4 _MainTex_ST;
CBUFFER_END
#define sampler_MainTex SamplerState_Trilinear_Repeat
SAMPLER(sampler_MainTex);

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float4 tangent : TANGENT;
    
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 worldPos : TEXCOORD1;

    float2 depth : TEXCOORD2;

};

v2f vert_shadow (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    o.depth = o.vertex.zw;
    return o;
}

float4 frag_sm (v2f i) : SV_Target
{
    // sample the texture
    // float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    float4 col = i.depth.x/i.depth.y;
    return col;
}

#endif