using EngineLib;

namespace AtomEngine
{
    [Documentation(DocumentationSection = "Attribute", Name = "GLDependableAttribute", Description = @"
    Атрибут используется на компонентах чтобы указать, что в компоненте используются GL- зависимые поля.
")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class GLDependableAttribute : Attribute
    {
    }
}
