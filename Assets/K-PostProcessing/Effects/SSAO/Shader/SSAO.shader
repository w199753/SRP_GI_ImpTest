Shader "F-PostProcessing/SSAO"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/XPostProcessing.hlsl"
	#include "../../../../Shaders/Montcalo_Library.hlsl"

	uniform int _SampleCount; 
	uniform float4x4 _InvProject;
	uniform float4x4 _InvView;

struct VaryingsDefault_SSAO
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float2 texcoordStereo : TEXCOORD1;
#if STEREO_INSTANCING_ENABLED
    uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
#endif
	float3 ray : TEXCOORD2;
};

VaryingsDefault_SSAO VertDefault_SSAO(AttributesDefault v)
{
    VaryingsDefault_SSAO o;
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
    o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

    o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);

    float far = _ProjectionParams.z;
    float4 clipPos = float4(o.texcoord * 2.0 - 1.0, 1.0, 1.0) * far; // 远截面的ndc * far = clip
    float4 viewRay = mul(_InvProject, clipPos);
    o.ray = viewRay.xyz;
    return o;
}

	half4 Frag(VaryingsDefault_SSAO i) : SV_Target
	{
        float3 N = float3(0,1,0); //used with z
        const uint SAMPLE_COUNT = 64u;
        float PDF = 0;
		for(uint idx=0;idx<SAMPLE_COUNT;idx++)
		{
			float2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
			float4 sm = CosineSampleHemisphere(Xi);
			PDF = sm.w;
			float3 H = TangentToWorld(sm.xyz,float4(N,1));
			//res += _EnvMap.SampleLevel(sampler_EnvMap, H, 0).rgb ;
		}
		half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
		float4 enc = SAMPLE_TEXTURE2D(_CameraDepthNormal, sampler_CameraDepthNormal, i.texcoord);
		float depth = 0;
		float3 normal;
		DecodeDepthNormal(enc,depth,normal);
		// sceneColor =
		        float3 viewPos = i.ray * (1.0-depth);
                // 这里的_InvV原本为：UNITY_MATRIX_I_V，但是shader时运行在后处理时
                // 所以MVP都被替换了，所以要用回原来主相机的MVP相关的矩阵都必须外部自己传进来
                float4 worldPos = mul(_InvView, float4(viewPos, 1));
				return worldPos;
				float3 rclipPos = mul((float3x3)unity_CameraProjection, viewPos);
			float2 rscreenPos = (rclipPos.xy / rclipPos.z) * 0.5 + 0.5;
			return rscreenPos.xyyy;
				return viewPos.xyzz;
		return worldPos/worldPos.w;
		return float4(i.texcoord,0,0);
		return float4(normal,depth);
		return sceneColor;
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault_SSAO
			#pragma fragment Frag

			ENDHLSL

		}
	}
}
