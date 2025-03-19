using EngineLib;

namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "ReadOnlyAttribute",
    Title = "Атрибут только для чтения",
    SubSection = "Attribute/Inspector",
    Description = @"
    Makes a field or component read-only in the inspector.

    namespace AtomEngine
    ReadOnlyAttribute()

    When this attribute is applied to a field or structure, it will be displayed in the inspector, but the user will not be able to change its value. This is useful for displaying information that should not be changed through the interface.

    The attribute can be applied to individual fields or entire components.

    Usage examples:
    public struct StatsComponent : IComponent
    {
        public string PlayerName;
        
        [ReadOnly]
        public int GamesPlayed;
    }
    
    [ReadOnly]
    public struct SystemInfoComponent : IComponent
    {
        public string Version;
        public DateTime BuildDate;
    }
    ",
    Author = "AtomEngine Team")]
    public class ReadOnlyAttribute : Attribute
    {
    }
}
