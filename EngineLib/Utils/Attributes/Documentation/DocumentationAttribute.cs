namespace EngineLib
{
    /// <summary>
    /// Attribute for documenting classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    [Documentation(
    Name = "DocumentationAttribute",
    Title = "Attribute for documenting classes",
    Description = @"
    Attribute for adding documentation to classes, structures, and other types.
    Used to automatically generate API documentation and reference materials.

    namespace AtomEngine
    DocumentationAttribute()

    Properties:
    - Name: The name of the element being documented
    - Description: A detailed description of the element
    - Author: The name of the element's author
    - Title: The title to display in the documentation
    - DocumentationSection: A section of the documentation (e.g. 'Component', 'Attribute', 'System')
    - SubSection: A subsection of the documentation for more detailed categorization

    Usage examples:
    [Documentation(
        Name = 'MyComponent',
        Description = 'Этот компонент делает что-то полезное',
        DocumentationSection = 'Component',
        SubSection = 'Rendering'
    )]
    public class MyRenderComponent { }
    ",
    Author = "AtomEngine Team",
    DocumentationSection = "Core",
    SubSection = "Attribute"
)]
    public class DocumentationAttribute : Attribute
    {
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string Author = string.Empty;
        public string Title = string.Empty;
        public string DocumentationSection = string.Empty;
        public string SubSection = string.Empty;
    }
}
