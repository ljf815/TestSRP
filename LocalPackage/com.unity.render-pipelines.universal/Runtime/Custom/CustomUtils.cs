using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Custom
{
    public static class CustomUtils 
    {
        [BurstCompile]
        static void GetViewBounds(Bounds bounds,Vector3 lightDirection,out Bounds viewBounds,out Quaternion invRot)
        {
            viewBounds = default;
            var halfExtend = bounds.extents;
            NativeArray<float3> points = new NativeArray<float3>(8, Allocator.Temp);
            points[0]=new float3(halfExtend.x,halfExtend.y,halfExtend.z);   
            points[1]=new float3(-halfExtend.x,halfExtend.y,halfExtend.z);  
            points[2]=new float3(halfExtend.x,-halfExtend.y,halfExtend.z);
            points[3]=new float3(-halfExtend.x,-halfExtend.y,halfExtend.z);
            points[4]=new float3(halfExtend.x,halfExtend.y,-halfExtend.z);
            points[5]=new float3(-halfExtend.x,halfExtend.y,-halfExtend.z);
            points[6]=new float3(halfExtend.x,-halfExtend.y,-halfExtend.z);
            points[7]=new float3(-halfExtend.x,-halfExtend.y,-halfExtend.z);

            var lightRotation = quaternion.LookRotation(lightDirection, new float3(0, 0, 1));
            invRot = math.inverse(lightRotation);
            for (int i = 0; i < points.Length; ++i)
            {
                viewBounds.Encapsulate(math.mul(invRot, points[i]));
            }
        
        }

        public static void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
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
    }
}