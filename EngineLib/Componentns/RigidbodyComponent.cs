using EngineLib;
using System.Numerics;

namespace AtomEngine
{
    public enum BodyType
    {
        Static,     
        Dynamic,    
        Kinematic   
    }

    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Physics",
    Name = "RigidbodyComponent",
    Description = @"
    Component for simulating the physical behavior of an entity.

    namespace AtomEngine

    Main properties:
    - Mass - the mass of the physical body
    - InverseMass - inverse mass (1/mass)
    - Inertia - inertia along three axes
    - InverseInertia - inverse inertia
    - BodyType - the type of the physical body (Dynamic, Static, Kinematic)

    Rigidbody automatically calculates inertia based on the body mass.

    For dynamic objects, the mass must be greater than zero.

    Body types:
    - Dynamic: fully subject to physics
    - Static: motionless and do not react to collisions
    - Kinematic: move programmatically, but affect dynamic bodies

    Usage example:
    var ball = world.CreateEntity();
    world.AddComponent<RigidbodyComponent>(ball);

    ",
    Author = "AtomEngine Team",
    Title = "Solid state component"
)]
    [TooltipCategoryComponent(ComponentCategory.Physic)]
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
