namespace AtomEngine
{
    public class WindowBuild
    {
        public const string AssembliesFolderName = "Libs";

        public readonly string BuildPath = string.Empty;
        public readonly string AssembliesPath = string.Empty;

        public WindowBuild(string appDomainDirectory = null)
        {
            BuildPath = appDomainDirectory == null ? AppDomain.CurrentDomain.BaseDirectory : appDomainDirectory;
            AssembliesPath = Path.Combine(BuildPath, AssembliesFolderName);
        }
    }
}
