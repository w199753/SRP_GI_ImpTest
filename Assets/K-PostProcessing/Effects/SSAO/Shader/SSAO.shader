Shader "F-PostProcessing/SSAO"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/XPostProcessing.hlsl"
	#include "../../../../Shaders/Montcalo_Library.hlsl"

	uniform int _SampleCount; 
    uniform int _OnlyShowAO;
    uniform float _ThicknessStrength;
    uniform float _SampleRange;
    uniform float _SampleBias;

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
        uint SAMPLE_COUNT = _SampleCount;
        float PDF = 0;
		// for(uint idx=0;idx<SAMPLE_COUNT;idx++)
		// {

		// 	//res += _EnvMap.SampleLevel(sampler_EnvMap, H, 0).rgb ;
		// }
		half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
		float4 enc = SAMPLE_TEXTURE2D(_CameraDepthNormal, sampler_CameraDepthNormal, i.texcoord);
		float src01Depth = 0;
		float3 scrViewNormal;
		DecodeDepthNormal(enc,src01Depth,scrViewNormal);
float oc = 0;
        float3 viewPos = i.ray * (1-src01Depth);
        for(int idx=0;idx<SAMPLE_COUNT;idx++)
        {
            float2 Xi = Hammersley(idx, SAMPLE_COUNT,HaltonSequence(idx));
			float4 sm = CosineSampleHemisphere(Xi);
			PDF = sm.w;
			float3 H = TangentToWorld(sm.xyz,float4(N,1));
            //if(H.z == 0)
            //H = normalize(float3(H.x,H.y,0.5));
            H = sm.xyz;
            
            // if(dot(H,scrViewNormal)<=0.05){
            //     H = normalize(TangentToWorld(H,float4(N,1)));
            // }
            H = dot(H,scrViewNormal)<0?-H:H;
            float3 v = viewPos + H * _ThicknessStrength;

            float4 rclipPos = mul(unity_CameraProjection, float4(v,1));
            float2 rscreenPos = (rclipPos.xy / rclipPos.w) * 0.5 + 0.5;

            float4 enc = SAMPLE_TEXTURE2D(_CameraDepthNormal, sampler_CameraDepthNormal, rscreenPos);
		    float r01Depth = 0;
		    float3 rViewNormal;
		    DecodeDepthNormal(enc,r01Depth,rViewNormal);
            //return r01Depth;
            //return r01Depth;
    
            float range = abs(r01Depth - src01Depth) * _ProjectionParams.z < _SampleRange ? 1.0 : 0.0;
            //return abs(src01Depth - r01Depth);
            range = smoothstep(0.0, 5.0, (_SampleRange) / max(0.0001,abs(src01Depth - r01Depth)));
            //return range;
            float ao = r01Depth + _SampleBias < src01Depth  ? 1.0 : 0.0;
            //return _SampleBias < src01Depth;
            //return ao;
           // return ao;
            oc += ao*range;
        }
		    oc = oc/SAMPLE_COUNT;
            oc = max(0.0, 1 - oc);
        if(_OnlyShowAO == 1)
            return oc;
        else
            return sceneColor*oc;
            // return oc; 
            //     // 这里的_InvV原本为：UNITY_MATRIX_I_V，但是shader时运行在后处理时
            //     // 所以MVP都被替换了，所以要用回原来主相机的MVP相关的矩阵都必须外部自己传进来
            //     float4 worldPos = mul(_InvView, float4(viewPos, 1));
			// 	return worldPos;
			// 	float3 rclipPos = mul((float3x3)unity_CameraProjection, viewPos);
			// float2 rscreenPos = (rclipPos.xy / rclipPos.z) * 0.5 + 0.5;
			// return rscreenPos.xyyy;
			// 	return viewPos.xyzz;
		    //return worldPos/worldPos.w;
		
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
