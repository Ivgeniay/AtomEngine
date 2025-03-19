using EngineLib;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    Name = "GLDependableAttribute",
    SubSection = "Attribute/Rendering",
    Description = @"
    Indicates that the component uses GL-dependent fields and requires the generation of service code.

    namespace AtomEngine
    GLDependableAttribute()

    This attribute is applied to public partial components implementing the IComponent interface
    to indicate to the code generator the need to create additional service fields for
    correct rendering in the graphics context.

    Important:
    - The component must be declared as partial
    - The component must implement the IComponent interface
    - The component must be public

    Examples of use:
    [GLDependable]
    public partial struct RenderComponent : IComponent
    {
        public Shader Shader;
        public Mesh Mesh;
        public Texture Texture;
    }
    ",
    Author = "AtomEngine Team",
    Title = "GL")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class GLDependableAttribute : Attribute
    {
    }
}
