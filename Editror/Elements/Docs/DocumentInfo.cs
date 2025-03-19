using System;

namespace Editor
{
    public class DocumentInfo
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Section { get; set; }
        public string SubSection { get; set; }
        public Type RelatedType { get; set; }
    }
}