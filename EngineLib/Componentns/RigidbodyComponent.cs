using System.Numerics;

namespace AtomEngine
{
    public enum BodyType
    {
        Static,     
        Dynamic,    
        Kinematic   
    }

    public struct RigidbodyComponent : IComponent
    {
        public Entity Owner { get; set; }

        public float Mass;                
        public float InverseMass;         
        public Vector3 Inertia;           
        public Vector3 InverseInertia;    
        public BodyType BodyType;

        public RigidbodyComponent(Entity owner, float mass, BodyType bodyType = BodyType.Dynamic)
        {
            Owner = owner;
            Mass = mass;
            InverseMass = mass > 0 ? 1.0f / mass : 0.0f;

            float i = 2.0f * mass / 5.0f;
            Inertia = new Vector3(i, i, i);
            InverseInertia = mass > 0 ? new Vector3(1.0f / i, 1.0f / i, 1.0f / i) : Vector3.Zero;
        }

        public override string ToString() => $"RIGIDBODY {Owner}: \n Mass: {Mass}, Inertia: {Inertia}, BodyType: {BodyType}";
    }


}
