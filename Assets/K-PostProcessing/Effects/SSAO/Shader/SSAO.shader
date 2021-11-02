Shader "F-PostProcessing/SSAO"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/XPostProcessing.hlsl"

	uniform int _SampleCount; 

	half4 Frag(VaryingsDefault i) : SV_Target
	{

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
