Shader "FRP/Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Normal("_Normal",2D) = "white"{}
        _RoughnessTex("_RoughnessTex",2D) = "white"{}
        _MetallicTex("_MetallicTex",2D) = "white"{}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "FRP_BASE"
            Tags{"LightMode" = "FRP_BASE"}
            HLSLPROGRAM
            #include "./FRP_Default.hlsl"
            #pragma shader_feature _NormalTexOn
            #pragma vertex vert
            #pragma fragment frag


            ENDHLSL
        }

        Pass
        {
            Name "FRP_Caster_Shadow"
            Tags{"LightMode" = "FRP_Caster_Shadow"}
            ColorMask R
            HLSLPROGRAM
            #include "./FRP_RSM.hlsl"
            #pragma shader_feature _NormalTexOn
            #pragma vertex vert_shadow
            #pragma fragment frag_sm


            ENDHLSL
        }

        Pass
        {
            Name "FRP_Caster_Normal"
            Tags{"LightMode" = "FRP_Caster_Normal"}
            ColorMask R
            HLSLPROGRAM
            #include "./FRP_RSM.hlsl"
            #pragma shader_feature _NormalTexOn
            #pragma vertex vert_shadow
            #pragma fragment frag_sm


            ENDHLSL
        }
    }
    CustomEditor "FRPShaderGUI"
}
