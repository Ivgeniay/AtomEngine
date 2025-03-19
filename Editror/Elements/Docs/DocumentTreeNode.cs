using System.Collections.Generic;

namespace Editor
{
    public class DocumentTreeNode
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsCategory { get; set; }
        public DocumentInfo Document { get; set; }
        public List<DocumentTreeNode> Children { get; } = new List<DocumentTreeNode>();
    }
}