namespace EngineLib
{
    [AttributeUsage(AttributeTargets.Class)]
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
