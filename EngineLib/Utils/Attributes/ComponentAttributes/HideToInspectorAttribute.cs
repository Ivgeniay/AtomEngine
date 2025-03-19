namespace EngineLib
{
    // <summary>
    /// Hides the component from being displayed in the inspector.
    /// </summary>
    [Documentation(
        DocumentationSection = "Core",
        SubSection = "Attribute/Inspector",
        Name = "HideToInspectorAttribute",
        Description = @"
    Hides the component from being displayed in the inspector.

    namespace AtomEngine
    HideToInspectorAttribute()

    This attribute is used for structures and components that should not be displayed in the inspector when viewing the entity. This can be useful for utility components that do not require configuration through the interface.

    Usage examples:
    [HideToInspector]
    public struct HiddenComponent : IComponent {}


    ",
        Author = "AtomEngine Team"
    )]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideToInspectorAttribute : Attribute
    {
    }

    /// <summary>
    /// Hides the component from search results in the inspector.
    /// </summary>
    [Documentation(
        DocumentationSection = "Core",
        SubSection = "Attribute/Inspector",
        Name = "HideInspectorSearchAttribute",
        Description = @"
    Hides the component from search results in the inspector.

    namespace AtomEngine
    HideInspectorSearchAttribute()

    Components with this attribute will be present in the inspector, but will not appear in search results. This can be useful

    for non-core components that should not clutter up search results, but still be accessible through direct access.

    Use examples:
    [HideInspectorSearch]
    public struct DebugComponent : IComponent
    {
        // Содержимое компонента
    }
    ",
        Author = "AtomEngine Team"
    )]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideInspectorSearchAttribute : Attribute
    {
    }

    /// <summary>
    /// Hides the delete button for the component in the inspector.
    /// </summary>
    [Documentation(
        DocumentationSection = "Core",
        SubSection = "Attribute/Inspector",
        Name = "HideCloseAttribute",
        Description = @"
    Hides the delete button for the component in the inspector.

    namespace AtomEngine
    HideCloseAttribute()

    Components with this attribute will not have a delete button in the inspector,
    which prevents them from being accidentally deleted by the user. This is especially useful

    for critical components that are required for the entity to function properly.

    Usage examples:
    [HideClose]
    public struct CoreComponent : IComponent
    {
        // Содержимое компонента
    }
    ",
        Author = "AtomEngine Team"
    )]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideCloseAttribute : Attribute
    {
    }
}
