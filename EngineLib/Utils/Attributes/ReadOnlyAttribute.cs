using EngineLib;

namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "HideInInspectorAttribute", Title = "Title", Description = @"
    Dont show this field in inspector
", SubSection = "Inspector")]
    public class ReadOnlyAttribute : Attribute
    {
    }
}
