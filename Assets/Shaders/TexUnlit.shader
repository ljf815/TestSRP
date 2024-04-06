Shader "Unlit/TexUnlit"
{
    Properties
    {
          [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
      
        _Color("Color", Color ) = (1.0,1.0,1.0,1.0)
        _Ratio("Ratio" ,Float) = 0.1
        _Tile("Tile",Float)=1
        _Polynomial("Polynomial",Vector)=(0,1,0,0)
        
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
                half4 _Polynomial;
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
                float t=input.uv.y;
                float v=t*_Tile;
               
              //  v=_Polynomial.x+v;
                v=_Polynomial.y*v+_Polynomial.z*v*v+_Polynomial.w*v*v*v;
                
                float dv=ddy(v);
                 float ratio=min(1,_Ratio*dv);
         
                float invDv=1/dv;
                float v2=v+dv;
                float fl1=floor(v);
                float fl2=floor(v2);
                float fr1=frac(v);
                float fr2=frac(v2);


                
                half4 color= _Color;
             //  color.a=(fl2-fl1)*invDv;
             //  color.a= (fl2-fl1)*_Ratio*invDv;//+ (GetRatio(fr2)- GetRatio(fr1))*invDv;//*0.5;// min(fr2,_Ratio);//-min(fr1,_Ratio);
              //  color.a=saturate(color.a);
             //   color.a*= ((fl2-fl1+1)*_Ratio-(GetRatio(fr1)+_Ratio-GetRatio(fr2)))/(fl2-fl1+1);
                color.a*=( min(fr2,ratio)+(fl2-fl1)*ratio+ -min(fr1,ratio))*invDv;
               
                return color;
            }
            ENDHLSL
        }
    }
     
}
