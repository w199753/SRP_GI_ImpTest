Shader "F-PostProcessing/SSAO"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/XPostProcessing.hlsl"
	#include "../../../../Shaders/Montcalo_Library.hlsl"

	uniform int _SampleCount; 

	half4 Frag(VaryingsDefault i) : SV_Target
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

		return sceneColor;
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag

			ENDHLSL

		}
	}
}
