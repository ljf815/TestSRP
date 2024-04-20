using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SGRenderFeature : ScriptableRendererFeature
{
    private SGShadowCasterPass m_ShadowCasterPass;
    public override void Create()
    {
       
        m_ShadowCasterPass = new SGShadowCasterPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ShadowCasterPass);
    }
}
