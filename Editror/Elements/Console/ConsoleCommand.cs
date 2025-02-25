using System;

namespace Editor
{
    public class ConsoleCommand
    {
        public string Description { get; set; } = string.Empty;
        public string CommandName { get; set; } = string.Empty;
        public Action<string> Action;
    }
}
