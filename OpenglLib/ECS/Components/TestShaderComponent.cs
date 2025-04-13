using AtomEngine;
using System.Numerics;

#if DEBUG
namespace OpenglLib
{
    public partial struct TestShaderComponent : IComponent
    {
        public Entity Owner { get; set; }

        public string UniformName;
        public bool IsFloat;
        public float FloatValue;
        public bool IsVector3;
        public Vector3 Vector3Value; 

        public TestShaderComponent(Entity owner) => Owner = owner;
    }
}
#endif
