using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Show private field in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "ShowInInspectorAttribute",
    SubSection = "Attribute/Inspector/Visible",
    Description = @"
    Shows a private field in the inspector.

    namespace AtomEngine
    ShowInInspectorAttribute()

    By default, private fields are not shown in the inspector. This attribute

    allows you to make private fields visible and editable in the inspector,

    while keeping them encapsulated in code.

    Usage examples:
    public struct ConfigComponent : IComponent
    {
        public string ConfigName;
        
        [ShowInInspector]
        private float _internalSetting;
    }
    ",
    Author = "AtomEngine Team")]
    public class ShowInInspectorAttribute : Attribute
    {
    }
}
