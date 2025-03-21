using EngineLib;

namespace AtomEngine
{
    [Documentation(
    Name = "IComponent",
    Title = "Component's base interface",
    Description = @"
    The base interface for all components in the ECS architecture.
    Defines the minimum set of properties that each component must have.
    Each component must have a reference to the owner entity (Entity).
    To work correctly with ECS, all components must implement this interface.

    Components must be declared as partial to enable automatic
    generation of service code and integration with engine systems.

    Example of use:
        
    public partial struct TransformComponent : IComponent 
    {
        public Entity Owner { get; set; }
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }
    ",
    Author = "AtomEngine Team",
    DocumentationSection = "Core",
    SubSection = "Components"
    )]
    public interface IComponent
    {
        Entity Owner { get; set; }
    }
}
