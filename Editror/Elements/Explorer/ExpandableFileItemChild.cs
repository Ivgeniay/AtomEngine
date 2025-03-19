using System.Collections.Generic;
using System;


namespace Editor
{
    public class ExpandableFileItemChild
    {
        public string ParentFilePath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public object Data { get; set; }
        public Func<ExpandableFileItemChild, string> GetDisplayName { get; set; }
        public int Level { get; set; } = 0;
        public List<ExpandableFileItemChild> Children { get; set; } = new List<ExpandableFileItemChild>();
        public bool IsExpanded { get; set; } = false;
        public string DisplayName => GetDisplayName?.Invoke(this) ?? Name;
    }
}