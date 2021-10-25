
#ifndef __FRP__DEFAULT__
#define __FRP__DEFAULT__

#include "./UnityHLSL.hlsl"
#include "./FRPLight.hlsl"
#include "./FRP_BRDF.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    TEXTURE2D(_MainTex);
    TEXTURE2D(_Normal);
    TEXTURE2D(_MetallicTex);
    float4 _MainTex_ST;
    float4 _Color;

    TEXTURE2D(_ShadowMapTex);
    TEXTURE2D(_RsmFlux);
    TEXTURE2D(_RsmWorldPos);
    TEXTURE2D(_RsmWorldNormal);
    //TEXTURE2D(_Normal);
    float4x4 _ShadowMapVP;
    int _RsmSampleCount;
    
CBUFFER_END


#define sampler_MainTex SamplerState_Trilinear_Repeat
SAMPLER(sampler_MainTex);

#define sampler_SMShadowMap SamplerState_Point_Clamp
SAMPLER(sampler_SMShadowMap);

StructuredBuffer<float3>_RsmRandomSamplePoint;

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
    float4 worldPos : TEXCOORD1;

    
    float3 tangent :TEXCOORD2;
    float3 bitangent :TEXCOORD3;
    float3 normal :TEXCOORD4;

    float4 sm_coord : TEXCOORD5;
};

uint ReverseBits32(uint bits)
{
	bits = (bits << 16) | (bits >> 16);
	bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
	bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
	bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
	bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
	return bits;
}

uint HaltonSequence(uint Index, uint base = 3)
{
	uint result = 0;
	uint f = 1;
	uint i = Index;
	
	[unroll(255)] 
	while (i > 0) {
		result += (f / base) * (i % base);
		i = floor(i / base);
	}
	return result;
}

float2 Hammersley(uint Index, uint NumSamples, uint2 Random)
{
	float E1 = frac((float)Index / NumSamples + float(Random.x & 0xffff) / (1 << 16));
	float E2 = float(ReverseBits32(Index) ^ Random.y) * 2.3283064365386963e-10;
	return float2(E1, E2);
}

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
    o.sm_coord = mul(_ShadowMapVP,mul(unity_ObjectToWorld,v.vertex));
    return o;
}

float4 frag (v2f i) : SV_Target
{
    _RsmSampleCount = 512;
    // sample the texture
    float4 rec_ndc = i.sm_coord/i.sm_coord.w ;
    float2 rec_uv = rec_ndc.xy*0.5 +0.5;
    
    float depth = transferDepth(rec_ndc.z);
    float sm_depth = SAMPLE_TEXTURE2D(_ShadowMapTex,sampler_SMShadowMap,rec_uv).r;
    //return sm_depth;
    //return min(max(0.0,step(depth-0.05,sm_depth)),1);
    float3 indirect=0;
    float shadow = min(max(0.0,step(depth-0.05,sm_depth)),1);
    //return shadow;
    //shadow = 1;
    float4 abledo = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    float4 resColor = 0;

    float3x3 tangentTransform = float3x3(i.tangent, i.bitangent, normalize(i.normal));
    half3 normal_Tex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_MainTex, i.uv));
    //return float4(normal_Tex,1);
    float Roughness = 0.75;
#if _NormalTexOn
    //float3 N = normalize(normal_Tex);//ormalize(mul(normal_Tex,tangentTransform));
    float3 N = normalize(mul(normal_Tex,tangentTransform));
#else
    //float3 N = normalize(i.tbn[2].xyz);
    float3 N = normalize(i.normal);
     //N = normalize(mul(normal_Tex,i.tbn));
#endif

#if _MetallicTexOn
    float Metallic = SAMPLE_TEXTURE2D(_MetallicTex,sampler_MainTex,i.uv).r;
#else
    float Metallic = 0.3;
#endif

    float3 F0 ;
    CalMaterialF0(abledo,Metallic,F0);

    float3 T = normalize(i.tangent);
    T = normalize(i.tangent - dot(i.tangent,N)*N);
    float3 B = normalize(i.bitangent);
    B = normalize(cross(N, T));
    float3 worldPos = i.worldPos.xyz;
    float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos);

    for(uint idx=0;idx<_RsmSampleCount;idx++)
    {
        float2 Xi = Hammersley(idx, _RsmSampleCount,HaltonSequence(idx));
        float ss1 = Xi.x*sin(Xi.y*2.0*3.1415926);
        float ss2 = Xi.x*cos(Xi.y*2.0*3.1415926);
		float2 sample_coord=rec_uv + float2(ss1,ss2) * (1.0/1024.0)*10;
		float weight=Xi.x*Xi.y;

		float3 vpl_normal=normalize(SAMPLE_TEXTURE2D(_RsmWorldNormal,sampler_SMShadowMap,sample_coord).xyz);
		float3 vpl_worldPos=SAMPLE_TEXTURE2D(_RsmWorldPos,sampler_SMShadowMap,sample_coord).xyz;
		float3 vpl_flux=SAMPLE_TEXTURE2D(_RsmFlux,sampler_SMShadowMap,sample_coord);

		float3 indirect_result = (vpl_flux*max(0, dot(vpl_normal, worldPos-vpl_worldPos))*max(0, dot(N, vpl_worldPos-worldPos)))/pow(length(worldPos-vpl_worldPos),2.0);
		indirect_result*=(weight*8);
		indirect+=indirect_result;
    }

    BRDFParam brdfParam;
    AnisoBRDFParam anisoBrdfParam;
    float4 dirCol = 0;
    half3 contrib = 0;
    for(int idx=0;idx< _LightCount;idx++)
    {
        Light light = _LightData[idx];
        float3 lightDir = 0;
        if(light.pos_type.w == 1)
        {
            contrib = CalDirLightContribution(light);
            lightDir = normalize(light.pos_type.xyz);
        }
        float3 L = lightDir;
        float3 H = normalize(V+L);
        InitBRDFParam(brdfParam,N,V,L,H);
        InitAnisoBRDFParam(anisoBrdfParam,T,B,H,L,V);
        dirCol += float4(contrib*shadow*BRDF_CookTorrance(abledo.rgb,F0,Metallic,Roughness,0,brdfParam,anisoBrdfParam),0);
        
    }


    float3 ks = F_SchlickRoughness(brdfParam.VdotH,F0,Roughness);
    float3 kd = (1.0-ks)*(1.0-Metallic);

    indirect = (indirect/_RsmSampleCount) * kd * abledo.xyz *1;
    indirect = 0;
    return float4(indirect + dirCol.xyz,1) ;

    // float4 xx = SAMPLE_TEXTURE2D(_ShadowMapTex,sampler_SMShadowMap,sm_uv);
    // return xx;
//     float3x3 tangentTransform = float3x3(i.tangent, i.bitangent, normalize(i.normal));
//     half3 normal_Tex = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_MainTex, i.uv));
// #if _NormalTexOn
//     //float3 N = normalize(normal_Tex);//ormalize(mul(normal_Tex,tangentTransform));
//     float3 N = normalize(mul(normal_Tex,tangentTransform));
// #else
//     //float3 N = normalize(i.tbn[2].xyz);
//     float3 N = normalize(i.normal);
//      //N = normalize(mul(normal_Tex,i.tbn));
// #endif
//     return N.xyzz;
    return abledo;
}

#endif