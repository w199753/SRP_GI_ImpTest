#ifndef __FRP_RSM__
#define __FRP_RSM__

#include "./UnityHLSL.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    TEXTURE2D(_MainTex);
    TEXTURE2D(_Normal);
    TEXTURE2D(_RoughnessTex);
    TEXTURE2D(_MetallicTex);
    float4 _MainTex_ST;
CBUFFER_END

#define sampler_MainTex SamplerState_Trilinear_Repeat
SAMPLER(sampler_MainTex);


float transferDepth(float z)
{
    float res = z;
#if defined (UNITY_REVERSED_Z)
	res = 1 - res;       //(1, 0)-->(0, 1)
#else 
	res = res*0.5 + 0.5; //(-1, 1)-->(0, 1)
#endif
    return res;
}

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

    float3 tangent :TEXCOORD3;
    float3 bitangent :TEXCOORD4;
    float3 normal :TEXCOORD5;

};

v2f vert_shadow (appdata v)
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
    o.depth = o.vertex.zw;
    return o;
}

float4 frag_sm (v2f i) : SV_Target
{
    float3x3 tangentTransform = float3x3(i.tangent, i.bitangent, normalize(i.normal));
    half3 normal_Tex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_MainTex, i.uv));
#if _NormalTexOn
    //float3 N = normalize(normal_Tex);//ormalize(mul(normal_Tex,tangentTransform));
    float3 N = normalize(mul(normal_Tex,tangentTransform));
#else
    //float3 N = normalize(i.tbn[2].xyz);
    float3 N = normalize(i.normal);
     //N = normalize(mul(normal_Tex,i.tbn));
#endif
    // sample the texture
    // float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    // return col;
    float depth = i.depth.x/i.depth.y;
    depth = transferDepth(depth);
    return float4(depth,0,0,0);
}

#endif