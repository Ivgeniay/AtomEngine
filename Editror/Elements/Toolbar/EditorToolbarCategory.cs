using System.Collections.Generic;

namespace Editor
{
    public class EditorToolbarCategory
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<EditorToolbarButton> Buttons { get; set; } = new List<EditorToolbarButton>();
    }

}