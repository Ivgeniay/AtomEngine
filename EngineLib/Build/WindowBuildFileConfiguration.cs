namespace AtomEngine
{
    public class WindowBuildFileConfiguration
    {
        public readonly string AssembliesFolderName = "Libs";
        public readonly string SceneExtension = "sc";
        public readonly string Data = "Data";
        public readonly string Scenes = "Scenes";

        public readonly string BuildPath = string.Empty;
        public readonly string AssembliesPath = string.Empty;
        public readonly string DataPath = string.Empty;
        public readonly string ScenesPath = string.Empty;

        public WindowBuildFileConfiguration(string appDomainDirectory = null)
        {
            BuildPath = appDomainDirectory == null ? AppDomain.CurrentDomain.BaseDirectory : appDomainDirectory;
            AssembliesPath = Path.Combine(BuildPath, AssembliesFolderName);
            DataPath = Path.Combine(BuildPath, Data);
            ScenesPath = Path.Combine(BuildPath, DataPath, Scenes);
        }
    }
}
