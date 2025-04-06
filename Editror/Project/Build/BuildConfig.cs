using AtomEngine;

namespace Editor
{
    internal class BuildConfig
    {
        public string ProjectName { get; set; } = "Game";
        public string OutputPath { get; set; } = string.Empty;
        public BuildPlatform TargetPlatform { get; set; } = BuildPlatform.Windows;
        public bool OptimizeResources { get; set; } = true;
        public bool IncludeDebugInfo { get; set; } = false;
        public string CompanyName { get; set; } = "My Company";
        public string Version { get; set; } = "1.0.0";
    }
}
