namespace AtomEngine
{
    public class WindowBuildFileConfiguration
    {
        public readonly string AssembliesFolderName = "Libs";
        public readonly string SceneExtension = "sc";
        public readonly string Data = "Data";
        public readonly string Scenes = "Scenes";
        public readonly string Resources = "Resources";
        public readonly string Textures = "Textures";
        public readonly string Models = "Models";
        public readonly string Materials = "Materials";
        public readonly string ResourceManifest = "resources.manifest";

        public readonly string BuildPath = string.Empty;
        public readonly string AssembliesPath = string.Empty;
        public readonly string DataPath = string.Empty;
        public readonly string ScenesPath = string.Empty;
        public readonly string ResourcesPath = string.Empty;
        public readonly string TexturesPath = string.Empty;
        public readonly string ModelsPath = string.Empty;
        public readonly string MaterialsPath = string.Empty;

        public WindowBuildFileConfiguration(string appDomainDirectory = null)
        {
            BuildPath = appDomainDirectory == null ? AppDomain.CurrentDomain.BaseDirectory : appDomainDirectory;
            AssembliesPath = Path.Combine(BuildPath, AssembliesFolderName);
            DataPath = Path.Combine(BuildPath, Data);
            ScenesPath = Path.Combine(BuildPath, DataPath, Scenes);

            ResourcesPath = Path.Combine(BuildPath, Resources);
            TexturesPath = Path.Combine(ResourcesPath, Textures);
            ModelsPath = Path.Combine(ResourcesPath, Models);
            MaterialsPath = Path.Combine(ResourcesPath, Materials);

            CreateDirectory(BuildPath);
            CreateDirectory(AssembliesPath);
            CreateDirectory(ScenesPath);
            CreateDirectory(ResourcesPath);
            CreateDirectory(TexturesPath);
            CreateDirectory(ModelsPath);
            CreateDirectory(MaterialsPath);
        }

        private void CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
