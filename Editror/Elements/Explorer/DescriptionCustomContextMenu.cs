using System;


namespace Editor
{
    public abstract class DescriptionCustomContextMenu
    {
        public string Name { get; set; } = string.Empty;
        public string[] SubCategory { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class DescriptionFileCustomContextMenu : DescriptionCustomContextMenu
    {
        public string Extension { get; set; } = string.Empty;
        public Action<FileSelectionEvent> Action { get; set; }
    }

    public class DescriptionFreeSpaceCustomContextMenu : DescriptionCustomContextMenu
    {
        public Action<string> Action { get; set; }
    }

    public class DescriptionDirectoryTreeCustomContextMenu : DescriptionCustomContextMenu
    {
        public Action<string> Action { get; set; }
    }

}