Shader "Unlit/TexUnlit"
{
    Properties
    {
          [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
      
        _Color("Color", Color ) = (1.0,1.0,1.0,1.0)
        _Ratio("Ratio" ,Range(0,1)) = 0.1
        _Tile("Tile",Float)=1
      
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

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseMap_ST variable, so that you
                // can use the _BaseMap variable in the fragment shader. The _ST
                // suffix is necessary for the tiling and offset function to work.
                float _Ratio;
                float _Tile;
                half4 _Color;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // The TRANSFORM_TEX macro performs the tiling and offset
                // transformation.
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float GetRatio(float fr)
            {
                return min(fr,_Ratio);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // The SAMPLE_TEXTURE2D marco samples the texture with the given
                // sampler.
              //  half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float v=input.uv.y*_Tile;
                float dv=ddy(v);
                float v2=v+dv;
                float fl1=floor(v);
                float fl2=floor(v2);
                float fr1=frac(v);
                float fr2=frac(v2);

                float p1=_Ratio- min(fr1,_Ratio);
                float p2= min(fr2,_Ratio);
                float total=(fl2-fl1+1)*_Ratio-p1-p2;
                total/=dv;
                
                
                half4 color= _Color;
               color.a=saturate(total);
               
                return color;
            }
            ENDHLSL
        }
    }
     
}
