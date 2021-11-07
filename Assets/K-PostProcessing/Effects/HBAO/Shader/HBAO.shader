Shader "F-PostProcessing/HBAO"
{

    HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/XPostProcessing.hlsl"
	#include "../../../../Shaders/Montcalo_Library.hlsl"

	uniform int _SampleCount; 
    uniform int _OnlyShowAO;
    uniform float _ThicknessStrength;
    // uniform float _SampleRange;
    // uniform float _SampleBias;

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

    VaryingsDefault_SSAO VertDefault_HBAO(AttributesDefault v)
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
        float2 uv = i.texcoord;
        float3 ray = i.ray;
		half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
		float4 enc = SAMPLE_TEXTURE2D(_CameraDepthNormal, sampler_CameraDepthNormal, i.texcoord);
		float src01Depth = 0;
		float3 scrViewNormal;
		DecodeDepthNormal(enc,src01Depth,scrViewNormal);
        float3 viewPos = i.ray * (1-src01Depth);
        const float radiusSS = 64.0 / 512.0;
        int directionsCount = _SampleCount;
        int stepsCount = _ThicknessStrength;

        float theta = 2.0 * PI / float(directionsCount);
        float2x2 deltaRotationMatrix = float2x2(
            cos(theta), -sin(theta),
            sin(theta),  cos(theta)
        );
        float2 deltaUV = float2(radiusSS / (stepsCount + 1.0), 0.0);
        float oc = 0;
        for (int i = 0; i < directionsCount; i++) {
            float horizonAngle = 0.04;
            deltaUV = mul(deltaRotationMatrix, deltaUV);

            for (int j = 1; j <= stepsCount; j++) {
                float2 sampleUV = uv + j * deltaUV;
                //float3 sampleVS = worldToView(screenToWorld(sampleUV));
                //float3 sampleDirVS = sampleVS - pointVS;

            float4 enc = SAMPLE_TEXTURE2D(_CameraDepthNormal, sampler_CameraDepthNormal, sampleUV);
		    float r01Depth = 0;
		    float3 rViewNormal;
		    DecodeDepthNormal(enc,r01Depth,rViewNormal);

            float3 sampleVS = ray * (1-r01Depth);
            float3 sampleDirVS = sampleVS - viewPos;
                float angle = (PI / 2.0) - acos(dot(scrViewNormal, normalize(sampleDirVS)));  
                if (angle > horizonAngle) {
                    float value = sin(angle) - sin(horizonAngle);
                    float attenuation = clamp(1.0 - pow(length(sampleDirVS) / 2.0, 2.0), 0.0, 1.0);
                    oc += value * attenuation;
                    horizonAngle = angle;
                }

            }
        }

                oc = 1.0 - oc / directionsCount;
                //oc = clamp(pow(oc, 2.7), 0.0, 1.0);
                //oc = pow(oc,1/2.2);
                //return 1;
                float3 outColor = oc;
                if(_OnlyShowAO == 1)
                return oc;
                else
                return sceneColor*oc;
                //outColor = pow(outColor, 1 / 2.2); // gamma correction
                //return outColor.xyzz;
	}
    ENDHLSL


    SubShader
    {
Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
			#pragma vertex VertDefault_HBAO
			#pragma fragment Frag
            ENDHLSL
        }
    }
}
