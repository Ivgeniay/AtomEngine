using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Dont show this field in inspector
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "HideInInspectorAttribute",
    SubSection = "Attribute/Inspector/Visible",
    Description = @"
    Hides the field in the inspector.

    namespace AtomEngine
    HideInInspectorAttribute()

    This attribute is used to exclude individual fields from being displayed in the inspector. This is useful for utility fields that should not be changed by the user directly, but should still be part of the component.

    Usage examples:
    public struct PlayerComponent : IComponent
    {
        public string Name;
        
        [HideInInspector]
        public Guid UniqueIdentifier;
    }
    ",
    Author = "AtomEngine Team")]
    public class HideInInspectorAttribute : Attribute
    {
    }
}
