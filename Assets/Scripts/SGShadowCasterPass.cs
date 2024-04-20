using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class SGShadowCasterPass : ScriptableRenderPass
{
    const int k_ShadowmapBufferBits = 16;
    private static int s_ShadowmapID = Shader.PropertyToID("_SGMAINLIGHTSHADOW");
    private bool m_ForceShadowPointSampling;

    class BlitData : ContextItem, IDisposable
    {
        private RTHandle m_ShadowmapTexture;
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler(nameof(SGShadowCasterPass));

        public BlitData()
        {
            Debug.Log("BlitData()");
        }
        class PassData
        {
            
        }
        static void Execute(PassData data, RasterGraphContext rgContext)
        {
           
        }

        public void Record(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;

            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_ShadowmapTexture, shadowData.mainLightShadowmapWidth,
                shadowData.mainLightShadowmapHeight, k_ShadowmapBufferBits, name: "_SGMainLightShadowmapTexture");

            using (var builder=renderGraph.AddRasterRenderPass<PassData>(nameof(SGShadowCasterPass),out var passData,m_ProfilingSampler))
            {
                var targetTexture= renderGraph.ImportTexture(m_ShadowmapTexture);
                builder.SetRenderAttachmentDepth(targetTexture);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc<PassData>(Execute);
            }

        }

        public override void Reset()
        {
            
        }

        public void Dispose()
        { 
            Debug.Log("release texture");
            m_ShadowmapTexture?.Release();
        }
    }

    public SGShadowCasterPass()
    {
        base.profilingSampler = new ProfilingSampler(nameof(SGShadowCasterPass));
        m_ForceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                                     GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);
        renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
        
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var blitData = frameData.Create<BlitData>();
        blitData.Record(renderGraph,frameData);
    }
}
