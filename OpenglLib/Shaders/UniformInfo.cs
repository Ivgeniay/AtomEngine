using Newtonsoft.Json;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    internal struct UniformInfo
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
}
