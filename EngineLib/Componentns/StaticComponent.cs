using EngineLib;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Physics",
    Name = "StaticComponent",
    Description = @"
    Component that marks an entity as static.

    namespace AtomEngine

    Static entities are not subject to physical interactions and do not move
    as a result of collisions. They are used to create stationary objects in the scene, such as the ground, walls, and other elements of the environment.

    Example of use:
    var wall = world.CreateEntity();
    world.AddComponent<StaticComponent>(wall);


    ",
    Author = "AtomEngine Team",
    Title = "Static object component"
)]
    [TooltipCategoryComponent(ComponentCategory.Physic)]
    public struct StaticComponent : IComponent
    {
        public Entity Owner { get; set; }
        public StaticComponent(Entity owner) => Owner = owner;
    }
}
