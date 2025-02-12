using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace OpenglLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LightingData_Sample
    {
        public int dirLightCount;
        public int pointLightCount;
    }
}
