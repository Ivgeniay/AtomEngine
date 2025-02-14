using System.Numerics;

namespace AtomEngine
{
    public struct CollisionComponent : IComponent
    {
        public Entity Owner { get; }
        public Entity CollidedWith;
        public Vector3 Normal;
        public float Penetration;
        public Vector3 ContactPoint;

        public CollisionComponent(Entity owner)
        {
            Owner = owner;
            CollidedWith = Entity.Null;
            Normal = Vector3.Zero;
            Penetration = 0;
            ContactPoint = Vector3.Zero;
        }
    }
}
