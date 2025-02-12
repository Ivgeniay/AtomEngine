using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace OpenglLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraData_TestMaterial
    {
        public Matrix4X4<float> viewMatrix;
        public Matrix4X4<float> projectionMatrix;
        public Vector3D<float> cameraPosition;
        public float nearPlane;
        public float farPlane;
    }
}
