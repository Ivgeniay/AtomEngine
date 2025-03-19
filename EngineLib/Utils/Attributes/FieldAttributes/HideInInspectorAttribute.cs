using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Dont show this field in inspector
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "HideInInspectorAttribute", Description = @"
    Dont show this field in inspector
", SubSection = "Inspector/Visible")]
    public class HideInInspectorAttribute : Attribute
    {
    }
}
