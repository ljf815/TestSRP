using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class SGShadowCasterPass : ScriptableRenderPass
{
    const int k_ShadowmapBufferBits = 16;
    private static int s_ShadowmapID = Shader.PropertyToID("_SGMAINLIGHTSHADOW");
    ProfilingSampler m_BuildSampler = new ProfilingSampler(nameof(SGShadowCasterPass)+":Build");
   static ProfilingSampler m_ExcuteSampler = new ProfilingSampler(nameof(SGShadowCasterPass)+"Execute");
    private static bool  m_ForceShadowPointSampling;
     LayerMask m_LayerMask;
     class PassData
     {
         public RendererListHandle rendererListHandle;
         
     }
    class BlitData : ContextItem, IDisposable
    {
        public RTHandle m_ShadowmapTexture;
        
        public override void Reset()
        {
            
        }

        public void Dispose()
        { 
            Debug.Log("release texture");
            m_ShadowmapTexture?.Release();
        }
    }

    public SGShadowCasterPass(LayerMask layerMask)
    {
        m_LayerMask = layerMask;
        base.profilingSampler = new ProfilingSampler(nameof(SGShadowCasterPass));
        m_ForceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                                     GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);
    
    }

    static void Execute(PassData data, RasterGraphContext rgContext)
    {
    //    using (new ProfilingScope(rgContext.cmd,m_ExcuteSampler))
        { 
            rgContext.cmd.ClearRenderTarget(true,false,Color.clear);
            rgContext.cmd.DrawRendererList(data.rendererListHandle);
        }
    }
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var blitData = frameData.Create<BlitData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
        var resourceData = frameData.Get<UniversalResourceData>();
        int shadowLightIndex = lightData.mainLightIndex;

        if (shadowLightIndex != 0)
        {
            Debug.Log(shadowLightIndex);
            return;
        }
            
        VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
        Light light = shadowLight.light;
        if (light.shadows == LightShadows.None)
        {
            return;
        }
        if (shadowLight.lightType != LightType.Directional)
        {
            Debug.LogWarning("Only directional lights are supported as main light.");
        }
            
 
        ShadowUtils.ShadowRTReAllocateIfNeeded(ref blitData.m_ShadowmapTexture, shadowData.mainLightShadowmapWidth,
            shadowData.mainLightShadowmapHeight, k_ShadowmapBufferBits, name: "_SGMainLightShadowmapTexture");
        
        // Access the relevant frame data from the Universal Render Pipeline
            
        var sortFlags = cameraData.defaultOpaqueSortFlags;
        RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
        FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, m_LayerMask);
        
        var shaderTagList = new List<ShaderTagId>{new("ShadowCaster")};
            
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagList, renderingData, cameraData, lightData, sortFlags);
       
       
        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        
        using (var builder=renderGraph.AddRasterRenderPass<PassData>(nameof(SGShadowCasterPass),out var passData,m_BuildSampler))
        {
            var targetTexture= renderGraph.ImportTexture(blitData.m_ShadowmapTexture);
            passData.rendererListHandle = renderGraph.CreateRendererList(param);
            builder.UseRendererList(passData.rendererListHandle);
            builder.SetRenderAttachmentDepth(targetTexture);
            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);
            if (targetTexture.IsValid())
                builder.SetGlobalTextureAfterPass(targetTexture, s_ShadowmapID);
            builder.SetRenderFunc<PassData>(Execute);
        }
    }
}
