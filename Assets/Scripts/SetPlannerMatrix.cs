using UnityEngine;

[ExecuteInEditMode]
public class SetPlannerMatrix : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      var localToWorld=   transform.localToWorldMatrix;
      var worldToLocal = localToWorld.inverse;
      Shader.SetGlobalMatrix("_PlaneSpace", localToWorld);
      Shader.SetGlobalMatrix("_PlaneSpeceInverse",worldToLocal);
    }
}
