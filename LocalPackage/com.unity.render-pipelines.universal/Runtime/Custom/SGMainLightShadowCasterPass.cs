using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Custom
{
    public class SGMainLightShadowCasterPass : ScriptableRenderPass
    {
        private bool mDebugDrawBound;
        private LayerMask m_LayerMask;
        
        Matrix4x4[] m_MainLightShadowMatrices;
        ShadowSliceData[] m_CascadeSlices;
        Vector4[] m_CascadeSplitDistances;
        
       static int m_MainLightShadowmapID = Shader.PropertyToID("_SGMainLightShadowmapTexture");
       ProfilingSampler m_ProfilingSetupSampler = new ProfilingSampler("Setup SG Main Shadowmap");
  
       const int k_MaxCascades = 4;
       const int k_ShadowmapBufferBits = 16;
       float m_CascadeBorder;
       float m_MaxShadowDistanceSq;
       int m_ShadowCasterCascadesCount;
       private bool m_CreateEmptyShadowmap;
       
       int renderTargetWidth;
       int renderTargetHeight;
        
        public SGMainLightShadowCasterPass(LayerMask layerMask, bool debugDrawBound)
        {
            mDebugDrawBound = debugDrawBound;
            m_LayerMask = layerMask;
        }
        
        private static class MainLightShadowConstantBuffer
        {
            public static int _WorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            public static int _ShadowParams = Shader.PropertyToID("_MainLightShadowParams");
            public static int _CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            public static int _CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            public static int _CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            public static int _CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            public static int _CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            public static int _ShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
            public static int _ShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
            public static int _ShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");
        }
        
        class BlitData : ContextItem, IDisposable
        {
            public RTHandle m_MainLightShadowmapTexture;
            public RTHandle m_EmptyLightShadowmapTexture;
            
            public override void Reset()
            {
                
            }

            public void Dispose()
            {
                 m_MainLightShadowmapTexture?.Release();
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            Setup(renderingData, cameraData, lightData, shadowData);

            var blitData = frameData.Create<BlitData>();

            if (m_CreateEmptyShadowmap)
                ShadowUtils.ShadowRTReAllocateIfNeeded(ref blitData.m_EmptyLightShadowmapTexture, 1, 1, k_ShadowmapBufferBits, name: "_SGEmptyLightShadowmapTexture");
            else
                ShadowUtils.ShadowRTReAllocateIfNeeded(ref blitData.m_MainLightShadowmapTexture, renderTargetWidth, renderTargetHeight, k_ShadowmapBufferBits, name: "_SGMainLightShadowmapTexture");
            
        }
        
        
         public bool Setup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            if (!shadowData.mainLightShadowsEnabled)
                return false;

            using var profScope = new ProfilingScope(m_ProfilingSetupSampler);

            if (!shadowData.supportsMainLightShadows)
                return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants);

            Clear();
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants);

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            if (light.shadows == LightShadows.None)
                return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants);

            if (shadowLight.lightType != LightType.Directional)
            {
                Debug.LogWarning("Only directional lights are supported as main light.");
            }

            Bounds bounds;
            if (!renderingData.cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants);

            m_ShadowCasterCascadesCount = shadowData.mainLightShadowCascadesCount;
            renderTargetWidth = shadowData.mainLightRenderTargetWidth;
            renderTargetHeight = shadowData.mainLightRenderTargetHeight;

            ref readonly URPLightShadowCullingInfos shadowCullingInfos = ref shadowData.visibleLightsShadowCullingInfos.UnsafeElementAt(shadowLightIndex);

            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                ref readonly ShadowSliceData sliceData = ref shadowCullingInfos.slices.UnsafeElementAt(cascadeIndex);
                m_CascadeSplitDistances[cascadeIndex] = sliceData.splitData.cullingSphere;
                m_CascadeSlices[cascadeIndex] = sliceData;

                if (!shadowCullingInfos.IsSliceValid(cascadeIndex))
                    return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants);
            }

           
            m_MaxShadowDistanceSq = cameraData.maxShadowDistance * cameraData.maxShadowDistance;
            m_CascadeBorder = shadowData.mainLightShadowCascadeBorder;
            m_CreateEmptyShadowmap = false;
            useNativeRenderPass = true;

            return true;
        }
         
        void Clear()
        {
            for (int i = 0; i < m_MainLightShadowMatrices.Length; ++i)
                m_MainLightShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
        }
        bool SetupForEmptyRendering(bool stripShadowsOffVariants)
        {
            if (!stripShadowsOffVariants)
                return false;

            m_CreateEmptyShadowmap = true;
            useNativeRenderPass = false;

       

            return true;
        }
    }
}