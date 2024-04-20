using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class SGShadowCasterPass : ScriptableRenderPass
{
    const int k_ShadowmapBufferBits = 16;
    private bool m_ForceShadowPointSampling;
    
    class PassData
    {
        public TextureHandle shadowmapTexture;
    }
    public SGShadowCasterPass()
    {
        base.profilingSampler = new ProfilingSampler(nameof(SGShadowCasterPass));
        m_ForceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                                     GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);

    }

    
     static void ExecutePass(PassData data, RasterGraphContext rgContext)
    {
        
    }
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        /*ShadowUtils.ShadowRTReAllocateIfNeeded( ref passData.shadowmapTexture, shadowData.mainLightShadowmapWidth,
            shadowData.mainLightShadowmapHeight, k_ShadowmapBufferBits, "_MainLightShadowmapTexture");
            */
        
        using (var builder=renderGraph.AddRasterRenderPass<PassData>(nameof(SGShadowCasterPass),out var passData,base.profilingSampler))
        {
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
 
                       var shadowTexDescrip = new TextureDesc
            {
                anisoLevel = 1,
                autoGenerateMips = true,
                bindTextureMS = false,//
                clearBuffer = false,//
                clearColor = Color.black,
                colorFormat = GraphicsFormat.None,
                depthBufferBits =DepthBits.Depth24 ,
                dimension = TextureDimension.Tex2D,
                disableFallBackToImportedTexture = false,//
                discardBuffer = false,
                enableRandomWrite = false,
                fastMemoryDesc = new FastMemoryDesc(),
                filterMode =  m_ForceShadowPointSampling? FilterMode.Point: FilterMode.Bilinear,
                func = null,
                width = shadowData.mainLightShadowmapWidth,
                height = shadowData.mainLightShadowmapHeight,
                isShadowMap = true,
                memoryless = RenderTextureMemoryless.None,
                mipMapBias = 0,
                msaaSamples = MSAASamples.None,
                name = "_SGShadowmapTexture",
                scale = default,
                sizeMode=TextureSizeMode.Explicit,
                slices = 1,
                useDynamicScale = false,
                useDynamicScaleExplicit = false,
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
            };
             
            passData.shadowmapTexture=renderGraph.CreateTexture(shadowTexDescrip);
         
            builder.SetRenderFunc<PassData>(ExecutePass);
        }
    }
}
