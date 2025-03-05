namespace EngineLib
{
    /// <summary>
    /// Usage for AddComponent navigation menu
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class TooltipCategoryComponentAttribute : Attribute
    {
        public readonly ComponentCategory ComponentCategory;
        public readonly string Description = string.Empty;

        public TooltipCategoryComponentAttribute(ComponentCategory category, string description = null) {
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
