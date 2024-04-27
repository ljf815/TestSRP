using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SGRenderFeature : ScriptableRendererFeature
{
    public bool DebugDrawBounds;
    public LayerMask ShadowLayerMask;
    public RenderPassEvent ShadowRenderPassEvent;
    private SGShadowCasterPass m_ShadowCasterPass;
    
    public override void Create()
    {
        m_ShadowCasterPass = new SGShadowCasterPass(ShadowLayerMask,DebugDrawBounds);
        m_ShadowCasterPass.renderPassEvent = ShadowRenderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ShadowCasterPass);
    }
}
