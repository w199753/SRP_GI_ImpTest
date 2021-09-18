
#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__

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

    float3 tangent :TEXCOORD2;
    float3 bitangent :TEXCOORD3;
    float3 normal :TEXCOORD4;

};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    float3 w_normal = TransformObjectToWorldNormal(v.normal);
    float3 w_tangent = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0)).xyz);
    float3 w_bitangent = cross(w_normal , w_tangent) * v.tangent.w;
    o.tangent = w_tangent;
    o.bitangent = w_bitangent;
    o.normal = w_normal;
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    return o;
}

float4 frag (v2f i) : SV_Target
{
    // sample the texture
    float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);

    return col;
}

#endif