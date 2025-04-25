

using System.Numerics;

namespace AtomEngine
{
    public partial struct TestRotateComponent : IComponent
    {
        public Entity Owner { get; set; }
        public Vector3 Axis;
        public float Speed;

        public TestRotateComponent(Entity entity) {
            Owner = entity;
            Axis = new Vector3() { X = 1, Y = 0, Z = 0 };
            Speed = 5;
        }
    }
}
