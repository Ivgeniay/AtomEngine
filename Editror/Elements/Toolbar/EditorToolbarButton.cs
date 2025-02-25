using System;

namespace Editor
{
    public class EditorToolbarButton
    {
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Action Action { get; set; }
    }

}