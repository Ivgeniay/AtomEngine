using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace OpenglLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraData_TestMaterial
    {
        public Matrix4X4<float> view;
        public Matrix4X4<float> projection;
        public Vector3D<float> cameraPos;
        public float padding;
    }
}
