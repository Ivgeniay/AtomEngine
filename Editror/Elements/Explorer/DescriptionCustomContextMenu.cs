using System;


namespace Editor
{
    public class DescriptionCustomContextMenu
    {
        public string Extension { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string[] SubCategory { get; set; }
        public string Description { get; set; } = string.Empty;
        public Action<FileSelectionEvent> Action { get; set; }
    }
}