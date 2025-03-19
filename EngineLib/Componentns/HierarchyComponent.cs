using EngineLib;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Transform",
    Name = "HierarchyComponent",
    Description = @"
    Component for organizing entities into a hierarchical tree structure.

    namespace AtomEngine

    Main properties:
    - Level - nesting level in the hierarchy
    - LocalIndex - local index among the child elements of the parent
    - Parent - parent entity identifier (uint.MaxValue for root objects)
    - Children - list of child entity identifiers

    Attributes:
    - [HideClose] - prevents accidental deletion of the component
    - [ReadOnly] - read-only fields in the inspector

    Features:
    - Root objects have the value Parent = uint.MaxValue
    - When adding, TransformComponent is automatically created if it is missing
    - Used for transformation in local/world space

    ",
    Author = "AtomEngine Team",
    Title = "Hierarchy Component"
)]
    [HideClose]
    public struct HierarchyComponent : IComponent
    {
        public Entity Owner { get ; set ;}
        [ReadOnly]
        public uint Level;
        [ReadOnly]
        public uint LocalIndex;
        [ReadOnly]
        public uint Parent;
        [ReadOnly]
        public List<uint> Children;
    }
}
