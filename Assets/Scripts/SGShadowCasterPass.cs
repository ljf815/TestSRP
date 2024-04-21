using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class SGShadowCasterPass : ScriptableRenderPass
{
    const int k_ShadowmapBufferBits = 16;
    private static int s_ShadowmapID = Shader.PropertyToID("_SGMAINLIGHTSHADOW");
     static  int s_unity_MatrixVP =Shader.PropertyToID("UNITY_MATRIXVP");
    ProfilingSampler m_BuildSampler = new ProfilingSampler(nameof(SGShadowCasterPass)+":Build");
   static ProfilingSampler m_ExcuteSampler = new ProfilingSampler(nameof(SGShadowCasterPass)+"Execute");
    private static bool  m_ForceShadowPointSampling;
     LayerMask m_LayerMask;
     class PassData
     {
         public RendererListHandle rendererListHandle;
         public Matrix4x4 vpMatrix4X4;
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
        using (new ProfilingScope(rgContext.cmd,m_ExcuteSampler))
        { 
             
            rgContext.cmd.SetGlobalMatrix(s_unity_MatrixVP,data.vpMatrix4X4);
            rgContext.cmd.ClearRenderTarget(true,false,Color.clear);
            rgContext.cmd.DrawRendererList(data.rendererListHandle);
        }
    }

    [BurstCompile]
    static void GetViewBounds(Bounds bounds,Vector3 lightDirection,out Bounds viewBounds,out Quaternion invRot)
    {
        viewBounds = default;
     
        var center = bounds.center;
        var halfExtend = bounds.extents;
        NativeList<float3> points = new NativeList<float3>(8, Allocator.Temp);
        points.Add(new float3(halfExtend.x,halfExtend.y,halfExtend.z));
        points.Add(new float3(-halfExtend.x,halfExtend.y,halfExtend.z));
        points.Add(new float3(halfExtend.x,-halfExtend.y,halfExtend.z));
        points.Add(new float3(-halfExtend.x,-halfExtend.y,halfExtend.z));
        points.Add(new float3(halfExtend.x,halfExtend.y,-halfExtend.z));
        points.Add(new float3(-halfExtend.x,halfExtend.y,-halfExtend.z));
        points.Add(new float3(halfExtend.x,-halfExtend.y,-halfExtend.z));
        points.Add(new float3(-halfExtend.x,-halfExtend.y,-halfExtend.z));

        var lightRotation = quaternion.LookRotation(lightDirection, new float3(0, 0, 1));
        invRot = math.inverse(lightRotation);
        for (int i = 0; i < points.Length; ++i)
        {
            viewBounds.Encapsulate(math.mul(invRot, points[i]));
        }
        
    }
    public void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);
 
        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));
 
        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));
 
        Debug.DrawLine(point1, point2, c);
        Debug.DrawLine(point2, point3, c);
        Debug.DrawLine(point3, point4, c);
        Debug.DrawLine(point4, point1, c);
 
        Debug.DrawLine(point5, point6, c);
        Debug.DrawLine(point6, point7, c);
        Debug.DrawLine(point7, point8, c);
        Debug.DrawLine(point8, point5, c);
 
        Debug.DrawLine(point1, point5, c);
        Debug.DrawLine(point2, point6, c);
        Debug.DrawLine(point3, point7, c);
        Debug.DrawLine(point4, point8, c);
        
    }
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
       
        Debug.Log(s_unity_MatrixVP);
        var blitData = frameData.Create<BlitData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
        var resourceData = frameData.Get<UniversalResourceData>();
        int shadowLightIndex = lightData.mainLightIndex;
 
        if (shadowLightIndex != 0)
        {
          //  Debug.Log(shadowLightIndex);
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
            return;
        }
 
        ShadowUtils.ShadowRTReAllocateIfNeeded(ref blitData.m_ShadowmapTexture, shadowData.mainLightShadowmapWidth,
            shadowData.mainLightShadowmapHeight, k_ShadowmapBufferBits, name: "_SGMainLightShadowmapTexture");
        
        // Access the relevant frame data from the Universal Render Pipeline
            
        var sortFlags = cameraData.defaultOpaqueSortFlags;
        RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
        FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, m_LayerMask);
       
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(new ShaderTagId("ShadowCaster"), renderingData, cameraData, lightData, sortFlags);
        
        var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
        param.cullingResults.GetShadowCasterBounds(shadowLightIndex, out Bounds bounds);
        GetViewBounds(bounds,light.transform.forward,out Bounds viewBounds,out Quaternion invRot);
        var viewMatrix = Matrix4x4.TRS(-bounds.center, invRot, Vector3.one);
        /*viewMatrix.m20 *= -1;
        viewMatrix.m21 *= -1;
        viewMatrix.m22 *= -1;
        viewMatrix.m23 *= -1;*/
        
        var extends = viewBounds.extents;
        var proj = Matrix4x4.Ortho(-extends.x, extends.x, -extends.y, extends.y, -extends.z, extends.z);
        proj = GL.GetGPUProjectionMatrix(proj, false);
        var vp = proj * viewMatrix;
        DrawBox(bounds.center,quaternion.identity,bounds.extents*2,Color.green);
        DrawBox(bounds.center,math.inverse(invRot),viewBounds.extents*2,Color.blue);
        
      var boundsObj = GameObject.Find("ShadowBounds");
        if (boundsObj != null)
        {
        //    boundsObj.transform.rotation.SetLookRotation(light.transform.forward,light.transform.up);
            boundsObj.transform.localPosition = bounds.center;
            boundsObj.transform.localScale = 2*bounds.extents;
            boundsObj.transform.rotation=quaternion.identity;
        }
        var projMatrix = Matrix4x4.identity;
       
        Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex,shadowData,  projMatrix,shadowData.mainLightShadowmapWidth);
        using (var builder=renderGraph.AddRasterRenderPass<PassData>(nameof(SGShadowCasterPass),out var passData,m_BuildSampler))
        {
            var targetTexture= renderGraph.ImportTexture(blitData.m_ShadowmapTexture);
            passData.rendererListHandle = renderGraph.CreateRendererList(param);
            passData.vpMatrix4X4 = vp;
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
