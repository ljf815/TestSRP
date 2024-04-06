Shader "Unlit/PlannerShadow"
{
    Properties
    {
 
        _Color("Color", Color ) = (1.0,1.0,1.0,1.0)
      
    }

       SubShader
    {
        Tags { "Queue" = "Transparent" "RanderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

         

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
            struct Attributes
            {
                float4 positionOS   : POSITION;
                
                // The uv variable contains the UV coordinate on the texture for the
                // given vertex.
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
             
                // The uv variable contains the UV coordinate on the texture for the
                // given vertex.
                float2 uv           : TEXCOORD0;
            };

            // This macro declares _BaseMap as a Texture2D object.
            TEXTURE2D(_BaseMap);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_BaseMap);

            float4x4 _PlaneSpace;
            float4x4 _PlaneSpeceInverse;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
             
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
              
                float3 position  = TransformObjectToWorld(input.positionOS.xyz);
                
                position=mul(_PlaneSpeceInverse,float4(position,1));
                position.y=0;
                position=mul(_PlaneSpace,float4(position,1));
            //    position=mul(planSpace,float4(position,1));
                // The TRANSFORM_TEX macro performs the tiling and offset
                // transformation.
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionHCS = TransformWorldToHClip(position);
                return output;
            }

          
            half4 frag(Varyings input) : SV_Target
            {
                
                
                
                
                return _Color;
            }
            ENDHLSL
        }
    }
     
}
