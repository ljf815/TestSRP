using System;
using UnityEngine;

//[ExecuteInEditMode]
public class Test1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      Test();
    }

    private void Update()
    {
      //  print("update");
    }

    // Update is called once per frame
    void Test()
    {
       var m= Camera.main.projectionMatrix;
    //   print(m);
       var cam = Camera.main;
       var size = cam.orthographicSize;
       var t = size;
       var b = -size;
       var l = -cam.aspect * size;
       var r = -l;
       var n = cam.nearClipPlane;
       var f = cam.farClipPlane;
       
       print(size);
       var reflect = Matrix4x4.identity;
       reflect.m22 = -1;

       var trans = new Matrix4x4(
           new Vector4(1, 0, 0, -(r + l) / 2),
           new Vector4(0, 1, 0, -(t + b) / 2),
           new Vector4(0,0,1,-(f+n)/2),
           new Vector4(0,0,0,1)).transpose;

       var scale = new Matrix4x4(
           new Vector4(2/(r-l),0,0,0),
           new Vector4(0,2/(t-b),0,0),
           new Vector4(0,0,2/(f-n),0),
           new Vector4(0,0,0,1)
       ).transpose;
       

       var proj =  scale* trans*reflect;
       print(m);
       print(proj);
       print(cam.worldToCameraMatrix);
      var m2= GL.GetGPUProjectionMatrix(m, false);
      print(m2);
    //  print(m2);
    }
}
