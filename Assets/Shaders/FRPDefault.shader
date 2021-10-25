Shader "FRP/Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color",Color) = (1,1,1,1)
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
            #pragma shader_feature _MetallicTexOn
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
            //#pragma shader_feature _NormalTexOn
            #pragma vertex vert_sm
            #pragma fragment frag_sm


            ENDHLSL
        }

        Pass
        {
            Name "FRP_Caster_Normal"
            Tags{"LightMode" = "FRP_Caster_Normal"}
            HLSLPROGRAM
            #include "./FRP_RSM.hlsl"
            #pragma shader_feature _NormalTexOn
            #pragma vertex vert_normal
            #pragma fragment frag_normal


            ENDHLSL
        }

        Pass
        {
            Name "FRP_Caster_Flux"
            Tags{"LightMode" = "FRP_Caster_Flux"}
            HLSLPROGRAM
            #include "./FRP_RSM.hlsl"
            #pragma vertex vert_flux
            #pragma fragment frag_flux

            ENDHLSL
        }

        Pass
        {
            Name "FRP_Caster_WorldPos"
            Tags{"LightMode" = "FRP_Caster_WorldPos"}
            HLSLPROGRAM
            #include "./FRP_RSM.hlsl"
            #pragma vertex vert_world
            #pragma fragment frag_world

            ENDHLSL
        }
    }
    CustomEditor "FRPShaderGUI"
}
