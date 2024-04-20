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
        public int Data;
    }
    public SGShadowCasterPass()
    {
        base.profilingSampler = new ProfilingSampler(nameof(SGShadowCasterPass));
        m_ForceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                                     GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);
        renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
        
    }

    
     static void ExecutePass(PassData data, RasterGraphContext rgContext)
    {
        
    }
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        /*ShadowUtils.ShadowRTReAllocateIfNeeded( ref passData.shadowmapTexture, shadowData.mainLightShadowmapWidth,
            shadowData.mainLightShadowmapHeight, k_ShadowmapBufferBits, "_MainLightShadowmapTexture");
            */
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
        var resourceData = frameData.Get<UniversalResourceData>();
        int shadowLightIndex = lightData.mainLightIndex;
        if (shadowLightIndex == -1)
            return;
        
        using (var builder=renderGraph.AddRasterRenderPass<PassData>(nameof(SGShadowCasterPass),out var passData,base.profilingSampler))
        {
           
            
                       var shadowTexDescrip = new TextureDesc
            {
                clearBuffer = true,//
                clearColor = Color.clear,
                depthBufferBits =DepthBits.Depth16 ,
                dimension = TextureDimension.Tex2D,
                filterMode =  m_ForceShadowPointSampling? FilterMode.Point: FilterMode.Bilinear,
                width = shadowData.mainLightShadowmapWidth,
                height = shadowData.mainLightShadowmapHeight,
                msaaSamples = MSAASamples.None,
                isShadowMap = true,
                name = "SGShadowmapTexture",
                sizeMode=TextureSizeMode.Explicit,
                slices = 1,
                wrapMode = TextureWrapMode.Clamp,
            };
            
            var shadowmapTexture=renderGraph.CreateTexture(shadowTexDescrip);
        //    builder.SetRenderAttachment(resourceData.activeColorTexture,0);
        var settings = new ShadowDrawingSettings(renderingData.cullResults, shadowLightIndex);
        settings.useRenderingLayerMaskTest = UniversalRenderPipeline.asset.useRenderingLayers;
     //   renderGraph.CreateShadowRendererList(ref settings);
            builder.SetRenderAttachmentDepth(shadowmapTexture,0);
         //   builder.AllowPassCulling(false); 
        //   builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc<PassData>(ExecutePass);
            
        }
    }
}
