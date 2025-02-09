using Silk.NET.Maths;

namespace OpenglLib
{
    public class PointLight : CustomStruct
    {
        public Vector3D<float> position { get; set; }
        public Vector3D<float> color { get; set; }
        public float ambient_strength { get; set; }
        public float intensity { get; set; }
        public float constant { get; set; }
        public float linear { get; set; }
        public float quadratic { get; set; }
    }
}
