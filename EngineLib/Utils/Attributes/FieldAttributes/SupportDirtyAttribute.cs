namespace EngineLib
{
    [Documentation(Author = "AtomEngine Team",
        SubSection = "Attribute/Inspector",
        Title = "SupportDirtyAttribute",
        DocumentationSection = "Core",
        Name = "SupportDirtyAttribute",
        Description = "Поакзывает испектору что у этого поля есть bool IsDirty"
        )]
    [AttributeUsage(AttributeTargets.Field)]
    public class SupportDirtyAttribute : Attribute { }
}
