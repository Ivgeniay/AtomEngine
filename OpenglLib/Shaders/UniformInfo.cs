using Newtonsoft.Json;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public struct UniformInfo
    {
        public int Location { get; set; }
        public int Size { get; set; }
        public UniformType Type { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public struct UniformSamplerInfo
    {
        public int? BindingPoint { get; set; } 
        public int Location { get; set; }
        public int Size { get; set; }
        public UniformType Type { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
