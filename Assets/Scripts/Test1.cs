using System;
using UnityEngine;

//[ExecuteInEditMode]
public class Test1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      //Test();
      TestPespective();
    }

    private void Update()
    {
      //  print("update");
    }

    void TestPespective()
    {
        var m= Camera.main.projectionMatrix;
        //   print(m);
        var cam = Camera.main;
        var fov = cam.fieldOfView;
        var tangent = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        var n = cam.nearClipPlane;
        var f = cam.farClipPlane;
        var t = n*tangent;
        var b = -t;
        var l = -cam.aspect * t;
        var r = -l;

        var invf = 1 / f;
        var invn = 1 / n;
     
        var reflect = Matrix4x4.identity;
        reflect.m22 = -1;
        
        

        var trans = new Matrix4x4(
            new Vector4(n, 0, 0, 0),
            new Vector4(0, n, 0,0),
            new Vector4(0,0,0,1),
            new Vector4(0,0,1,0)).transpose;
        

        var scale = new Matrix4x4(
            new Vector4(2/(r-l),0,0,-(r+l)/(r-l)),
            new Vector4(0,2/(t-b),0,-(t+b)/(t-b)),
            new Vector4(0,0,2/(invf-invn),-(invf+invn)/(invf-invn)),
            new Vector4(0,0,0,1)
        ).transpose;
       
        var dx = new Matrix4x4(
            new Vector4(1,0,0,0),
            new Vector4(0,1,0,0),
            new Vector4(0,0,-0.5f,0.5f),
            new Vector4(0,0,0,1)
        ).transpose;
        
        var proj =  scale* trans*reflect;
        print(m);
       
        print(proj);
        print(dx*m);
        
        print(cam.worldToCameraMatrix);
        var m2= GL.GetGPUProjectionMatrix(m, false);
        print(m2);
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
