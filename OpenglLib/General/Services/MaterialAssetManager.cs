using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class MaterialAssetManager : IService
    {
        public const string MATERIAL_EXT = ".mat";
        public const string MATERIAL_EXT_MASK = "*.mat";

        protected EventHub eventHub;
        protected Dictionary<string, MaterialAsset> _cacheMaterialAssets = new Dictionary<string, MaterialAsset>();

        public virtual Task InitializeAsync()
        {
            eventHub = ServiceHub.Get<EventHub>();
            return Task.CompletedTask;
        }

        public MaterialAsset CreateMaterialAsset(string shaderRepresentationGuid, string directory = null, string nameWithoutExt = null)
        {
            string filePath = ServiceHub.Get<MetadataManager>().GetPathByGuid(shaderRepresentationGuid);

            if (File.Exists(filePath))
            {
                if (string.IsNullOrWhiteSpace(nameWithoutExt)) nameWithoutExt = $"Material_{Guid.NewGuid().ToString().Substring(0, 8)}";
                if (string.IsNullOrWhiteSpace(directory))
                {
                    string filename = Path.GetFileNameWithoutExtension(filePath);
                    directory = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        $"{nameWithoutExt}.mat"
                    );
                }
                var material = CreateEmptyMaterialAsset(directory, nameWithoutExt);
                AssignShaderToMaterial(material, shaderRepresentationGuid);
            }
            else
            {
                DebLogger.Warn($"Shader representation file not found: GUID={shaderRepresentationGuid}");
            }
            return null;
        }

        public MaterialAsset? CreateEmptyMaterialAsset(string directory, string nameWithoutExt = null)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                DebLogger.Error($"Creating new material error: directory is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(nameWithoutExt))
                nameWithoutExt = $"Material_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var material = new MaterialAsset();

            string path = Path.Combine(directory, $"{nameWithoutExt}{MATERIAL_EXT}");

            SaveMaterialAsset(material, path);
            _cacheMaterialAssets[path] = material;
            return material;
        }


        public void AssignShaderToMaterial(MaterialAsset material, string shaderRepresentationGuid)
        {
            if (material == null)
            {
                DebLogger.Error("Error assign shader to material. Material asset is not exist");
                return;
            }

            MetadataManager metadataManager = ServiceHub.Get<MetadataManager>();
            string filePath = metadataManager.GetPathByGuid(shaderRepresentationGuid);

            if (!File.Exists(filePath))
            {
                DebLogger.Warn($"Shader representation file not found: GUID={shaderRepresentationGuid}");
                return;
            }

            if (Path.HasExtension(filePath))
            {
                var extension = Path.GetExtension(filePath);
                if (extension.Contains("cs"))
                {
                    AssignShaderToMaterialFromCS(material, shaderRepresentationGuid, metadataManager, filePath);
                }

                if (extension.Contains("glsl"))
                {

                }
            }
        }

        private void AssignShaderToMaterialFromCS(MaterialAsset material, string shaderRepresentationGuid, MetadataManager metadataManager, string filePath)
        {
            material.ShaderRepresentationGuid = shaderRepresentationGuid;

            try
            {
                string fileContent = File.ReadAllText(filePath);
                string namespaceName = CSRepresentationParser.ExtractNamespace(fileContent);
                string className = CSRepresentationParser.ExtractClassName(fileContent);

                if (!string.IsNullOrEmpty(namespaceName) && !string.IsNullOrEmpty(className))
                {
                    material.ShaderRepresentationTypeName = $"{namespaceName}.{className}";
                }
                else
                {
                    DebLogger.Warn($"Could not extract namespace or class name from file: {filePath}");
                    DefaultGettingRepTymeName(material, filePath);
                }

                var uniformContainers = ShaderRepresentationAnalyzer.AnalyzeShaderRepresentation(material.ShaderRepresentationTypeName, fileContent);
                foreach (var container in uniformContainers)
                {
                    material.AddContainer(container);
                }

                string path = GetPathFromAsset(material);
                if (!string.IsNullOrEmpty(path))
                {
                    var materialMeta = metadataManager.GetMetadata(path);
                    if (materialMeta == null || !materialMeta.Dependencies.Contains(shaderRepresentationGuid))
                    {
                        var assetDepencyManager = ServiceHub.Get<AssetDependencyManager>();
                        if (materialMeta != null && materialMeta.Dependencies.Count() > 0)
                        {
                            string[] temp = new string[materialMeta.Dependencies.Count()];
                            materialMeta.Dependencies.CopyTo(temp);

                            for (int i = 0; i < materialMeta.Dependencies.Count(); i++)
                            {
                                assetDepencyManager.RemoveDependencyFromPathByGuid(path, temp[i]);
                            }
                        }
                        assetDepencyManager.AddDependencyFromPathByGuid(path, shaderRepresentationGuid);
                    }
                    SaveMaterialAsset(material, path);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error processing shader representation file: {ex.Message}");
            }
        }

        public virtual void SaveMaterialAsset(MaterialAsset material)
        {
            string path = _cacheMaterialAssets.Where(e => e.Value.Guid == material.Guid).FirstOrDefault().Key;
            if (path != null)
            {
                SaveMaterialAsset(material, path);
            }
            else
            {
                DebLogger.Error($"Saving material {material.Guid}. Unkown path to safe");
            }
        }
        public virtual void SaveMaterialAsset(MaterialAsset material, string path)
        {
            string json = MaterialSerializer.SerializeMaterial(material);
            File.WriteAllText(path, json);
        }

        protected virtual MaterialAsset LoadMaterial(string path)
        {
            string json = FileLoader.LoadFile(path);
            MaterialAsset asset = MaterialSerializer.DeserializeMaterial(json);
            //var t = MaterialSerializer.SerializeMaterial(asset);
            _cacheMaterialAssets[path] = asset;

            return asset;
        }

        public virtual MaterialAsset GetMaterialAssetByPath(string path)
        {
            if (!File.Exists(path))
            {
                if (_cacheMaterialAssets.TryGetValue(path, out MaterialAsset mat)) _cacheMaterialAssets.Remove(path);

                DebLogger.Error($"File {path} is not exist");
                return null;
            }

            if (_cacheMaterialAssets.TryGetValue(path, out MaterialAsset material))
            {
                return material;
            }

            return LoadMaterial(path);
        }
        public virtual MaterialAsset GetMaterialAssetFromGUID(string guid) =>
            _cacheMaterialAssets.FirstOrDefault(e => e.Value.Guid == guid).Value;
        public virtual string GetPathFromGUID(string guid) =>
            _cacheMaterialAssets.FirstOrDefault(e => e.Value.Guid == guid).Key;
        public virtual string GetPathFromAsset(MaterialAsset material) =>
            _cacheMaterialAssets.Where(e => e.Value.Equals(material)).FirstOrDefault().Key;

        public IEnumerable<(string, MaterialAsset)> GetMaterials()
        {
            foreach(var kvp in _cacheMaterialAssets)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }

        public (string, string) GetDefaulShaderValue()
        {
            return (string.Empty, string.Empty);
        }

        protected void DefaultGettingRepTymeName(MaterialAsset material, string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.IndexOf(".") > -1) fileName = fileName.Substring(0, fileName.IndexOf("."));
            material.ShaderRepresentationTypeName = $"OpenglLib.{fileName}";
        }
        protected void CacheAllMaterials(string rootDirectory)
        {
            try
            {
                var matFiles = Directory.GetFiles(rootDirectory, MATERIAL_EXT_MASK, SearchOption.AllDirectories);
                foreach (var matFile in matFiles)
                {
                    try
                    {
                        var material = LoadMaterial(matFile);
                        if (material != null)
                        {
                            DebLogger.Debug($"Cached material: {matFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Warn($"Failed to cache material {matFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error while scanning for materials: {ex.Message}");
            }
        }

    }

}
