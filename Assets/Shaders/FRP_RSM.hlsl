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
    float4 _Color;
CBUFFER_END

#define sampler_MainTex SamplerState_Point_Clamp
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

struct appdata_sm
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;    
};

struct v2f_sm
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float2 depth : TEXCOORD1;
};

v2f_sm vert_sm (appdata_sm v)
{
    v2f_sm o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    o.depth = o.vertex.zw;
    return o;
}

float4 frag_sm (v2f_sm i) : SV_Target
{

    // sample the texture
    // float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    // return col;
    float depth = i.depth.x/i.depth.y;
    depth = transferDepth(depth);
    return float4(depth,0,0,0);
}

//---------------------------------------------
struct appdata_normal
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float4 tangent : TANGENT;
    
};

struct v2f_normal
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;

    float2 depth : TEXCOORD2;

    float3 tangent :TEXCOORD3;
    float3 bitangent :TEXCOORD4;
    float3 normal :TEXCOORD5;

};

v2f_normal vert_normal(appdata_normal v)
{
    v2f_normal o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    float3 w_normal = TransformObjectToWorldNormal(v.normal);
    float3 w_tangent = normalize(mul(unity_ObjectToWorld,float4(v.tangent.xyz,0)).xyz);
    float3 w_bitangent = cross(w_normal , w_tangent) * v.tangent.w;
    o.tangent = w_tangent;
    o.bitangent = w_bitangent;
    o.normal = w_normal;
    return o;
}

float4 frag_normal(v2f_normal i):SV_TARGET
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
    return N.xyzz;
}

//---------------------------------------------
struct appdata_flux
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;    
};

struct v2f_flux
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

v2f_flux vert_flux (appdata_flux v)
{
    v2f_flux o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    return o;
}

float4 frag_flux (v2f_flux i) : SV_Target
{
    // sample the texture
    float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    return col*_Color;
}

//---------------------------------------------
struct appdata_world
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;    
};

struct v2f_world
{
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float4 vertex : SV_POSITION;
};

v2f_world vert_world (appdata_world v)
{
    v2f_world o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.worldPos = mul(unity_ObjectToWorld,v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
    return o;
}

float4 frag_world (v2f_world i) : SV_Target
{
    // sample the texture
    float3 worldPos = i.worldPos;
    return float4(worldPos,1);
}

#endif