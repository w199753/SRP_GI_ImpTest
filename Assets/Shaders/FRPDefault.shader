Shader "FRP/Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            
            #pragma vertex vert
            #pragma fragment frag


            ENDHLSL
        }

        Pass
        {
            Name "FRP_Caster_RSM"
            Tags{"LightMode" = "FRP_Caster_RSM"}
            HLSLPROGRAM
            #include "./FRP_RSM.hlsl"
            
            #pragma vertex vert_shadow
            #pragma fragment frag_sm


            ENDHLSL
        }
    }
}
