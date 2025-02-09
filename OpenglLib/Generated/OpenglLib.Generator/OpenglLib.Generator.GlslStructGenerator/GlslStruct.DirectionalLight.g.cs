using Silk.NET.Maths;

namespace OpenglLib
{
    public class DirectionalLight : CustomStruct
    {
        public Vector3D<float> direction { get; set; }
        public Vector3D<float> color { get; set; }
        public float ambient_strength { get; set; }
        public float intensity { get; set; }
    }
}
