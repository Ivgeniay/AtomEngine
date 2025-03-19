namespace EngineLib
{
    /// <summary>
    /// Dont show this component in inspector
    /// </summary>
    [Documentation(DocumentationSection = "Attribute", Name = "HideToInspectorAttribute", Description = @"
    Dont show this component in inspector.
", SubSection = "Inspector")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideToInspectorAttribute : Attribute
    {
    }

    /// <summary>
    /// Dont show this component in inpector search
    /// </summary>
    [Documentation(DocumentationSection = "Attribute", Name = "HideInspectorSearchAttribute", Description = @"
    Dont show this component in inpector search.
", SubSection = "Inspector")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideInspectorSearchAttribute : Attribute
    {
    }

    /// <summary>
    /// Dont show delete button for this component
    /// </summary>
    [Documentation(DocumentationSection = "Attribute", Name = "HideCloseAttribute", Description = @"
    Dont show delete button for this component.
", SubSection = "Inspector")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideCloseAttribute : Attribute
    {
    }
}
