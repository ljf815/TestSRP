Shader "Test1"
{
 
    Properties
    {
        _Color("Color",Color)=(1,1,1,1)
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
    }
 
    SubShader
    {
 
        Tags { "Queue" = "Geometry"   "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Plane" }

        Pass
        {
            AlphaToMask [_AlphaToMask]
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };


            Varyings vert(Attributes input)
            {
                Varyings output;
                float4 hclip=TransformObjectToHClip(input.positionOS.xyz);
                output.positionHCS=hclip;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
               return input.positionHCS.y/_ScreenParams.y;
            }
            ENDHLSL
        }
    }
    
}