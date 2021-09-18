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
    }
}
