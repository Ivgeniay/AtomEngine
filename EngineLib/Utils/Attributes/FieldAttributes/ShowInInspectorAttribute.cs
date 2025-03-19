using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Show private field in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "ShowInInspectorAttribute", Description = @"
    Show private field in inspector
", SubSection = "Inspector/Visible")]
    public class ShowInInspectorAttribute : Attribute
    {
    }
}
