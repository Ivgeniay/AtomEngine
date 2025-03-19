namespace EngineLib
{
    /// <summary>
    /// Usage for AddComponent navigation menu
    /// </summary>
    /// <summary>
    /// Определяет категорию и описание компонента для меню добавления компонентов
    /// </summary>
    [Documentation(
        DocumentationSection = "Core",
        Name = "TooltipCategoryComponentAttribute",
        SubSection = "Attribute/Inspector",
        Description = @"
    Defines the category and description of the component for the AddComponent menu.

    namespace AtomEngine
    TooltipCategoryComponentAttribute(ComponentCategory category, string description = null)

    This attribute is used to categorize components in the AddComponent navigation menu,
    which allows you to logically group components and add additional descriptions to them.
    This makes it easier to find and use components in the editor.

    Parameters:
    - category: Component category from the ComponentCategory enumeration
    - description: Optional text description of the component that will be displayed in the tooltip

    Usage examples:
    [TooltipCategoryComponent(ComponentCategory.Rendering, ""Обрабатывает графическое отображение сущности"")]
    public struct RenderComponent : IComponent { }
    ",
    Author = "AtomEngine Team")]
[AttributeUsage(AttributeTargets.Struct)]
    public class TooltipCategoryComponentAttribute : Attribute
    {
        public readonly ComponentCategory ComponentCategory;
        public readonly string Description = string.Empty;
        public TooltipCategoryComponentAttribute(ComponentCategory category, string description = null)
        {
            ComponentCategory = category;
            if (description != null) Description = description;
        }
    }

    public enum ComponentCategory
    {
        None,
        Physic,
        Render,
        Genearal,
        Other
    }
}
