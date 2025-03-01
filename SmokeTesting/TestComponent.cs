using AtomEngine;
using AtomEngine.RenderEntity;
using System.Numerics;

namespace SmokeTesting
{
    public partial struct TestComponent : IComponent
    {
        public Entity Owner {  get; set; }
        public ShaderBase Shader { get; set; }
    }
}
