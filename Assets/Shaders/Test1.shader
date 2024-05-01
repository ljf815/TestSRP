Shader "Test1"
{
 
    Properties
    { }
 
    SubShader
    {
 
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
               return half4(1,0,0,1);
            }
            ENDHLSL
        }
    }
}