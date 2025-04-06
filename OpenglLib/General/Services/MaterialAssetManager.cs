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

                var material = new MaterialAsset
                {
                    ShaderRepresentationGuid = shaderRepresentationGuid,
                };

                try
                {
                    FillMaterialDataFromShader(material, filePath);

                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        string filename = Path.GetFileNameWithoutExtension(filePath);
                        directory = Path.Combine(
                            Path.GetDirectoryName(filePath),
                            $"{nameWithoutExt}.mat"
                        );
                    }

                    SaveMaterialAsset(material, directory);

                    _cacheMaterialAssets[directory] = material;
                    return material;
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Error parsing shader representation file: {ex.Message}");
                    DefaultGettingRepTymeName(material, filePath);
                }
            }
            else
            {
                DebLogger.Warn($"Shader representation file not found: GUID={shaderRepresentationGuid}");
            }
            return null;
        }

        private void FillMaterialDataFromShader(MaterialAsset material, string shaderFilePath)
        {
            string fileContent = File.ReadAllText(shaderFilePath);
            string namespaceName = CSRepresentationParser.ExtractNamespace(fileContent);
            string className = CSRepresentationParser.ExtractClassName(fileContent);


            if (!string.IsNullOrEmpty(namespaceName) && !string.IsNullOrEmpty(className))
            {
                material.ShaderRepresentationTypeName = $"{namespaceName}.{className}";
                DebLogger.Info($"Set shader representation type: {material.ShaderRepresentationTypeName}");
            }
            else
            {
                DebLogger.Warn($"Could not extract namespace or class name from file: {shaderFilePath}");
                DefaultGettingRepTymeName(material, shaderFilePath);
            }

            InitializeUniformsFromShaderRepresentation(material, shaderFilePath);
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

            string filePath = ServiceHub.Get<MetadataManager>().GetPathByGuid(shaderRepresentationGuid);

            if (!File.Exists(filePath))
            {
                DebLogger.Warn($"Shader representation file not found: GUID={shaderRepresentationGuid}");
                return;
            }

            material.ShaderRepresentationGuid = shaderRepresentationGuid;

            try
            {
                string fileContent = File.ReadAllText(filePath);
                string namespaceName = CSRepresentationParser.ExtractNamespace(fileContent);
                string className = CSRepresentationParser.ExtractClassName(fileContent);

                if (!string.IsNullOrEmpty(namespaceName) && !string.IsNullOrEmpty(className))
                {
                    material.ShaderRepresentationTypeName = $"{namespaceName}.{className}";
                    DebLogger.Info($"Set shader representation type: {material.ShaderRepresentationTypeName}");
                }
                else
                {
                    DebLogger.Warn($"Could not extract namespace or class name from file: {filePath}");
                    DefaultGettingRepTymeName(material, filePath);
                }

                InitializeUniformsFromShaderRepresentation(material, filePath);

                string path = GetPathFromAsset(material);
                if (!string.IsNullOrEmpty(path))
                {
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
            string path = _cacheMaterialAssets.Where(e => e.Value == material).FirstOrDefault().Key;
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

        public virtual void InitializeUniformsFromShaderRepresentation(MaterialAsset material, string shaderRepresentationPath)
        {
            if (!File.Exists(shaderRepresentationPath))
            {
                DebLogger.Error($"Shader representation file not found: {shaderRepresentationPath}");
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(shaderRepresentationPath);

                //if (material.UniformValues == null)
                material.UniformValues = new Dictionary<string, object>();

                //if (material.TextureReferences == null)
                material.TextureReferences = new Dictionary<string, string>();

                Dictionary<string, object> properties = new Dictionary<string, object>();
                List<string> samplers = new List<string>();
                CSRepresentationParser.ExtractUniformProperties(fileContent, properties, samplers);
                foreach (var pair in properties)
                {
                    if (!samplers.Contains(pair.Key))
                        material.UniformValues[pair.Key] = pair.Value;
                }
                foreach (var sampler in samplers)
                {
                    material.TextureReferences.Add(sampler, string.Empty);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error analyzing shader representation: {ex.Message}");
            }
        }
        public T GetUniformValue<T>(MaterialAsset material, string name)
        {
            if (material.UniformValues.TryGetValue(name, out var value))
            {
                string typeName = typeof(T).Name;
                object convertedValue = ConvertJObjectToTypedValue(value, typeName);
                if (convertedValue is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(convertedValue, typeof(T));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }

        public IEnumerable<(string, MaterialAsset)> GetMaterials()
        {
            foreach(var kvp in _cacheMaterialAssets)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }

        protected object ConvertJObjectToTypedValue(object value, string typeName)
        {
            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                switch (typeName)
                {
                    case "vec2":
                    case "Vector2D<float>":
                        float x = jObject["X"]?.ToObject<float>() ?? 0f;
                        float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector2D<float>(x, y);

                    case "vec3":
                    case "Vector3D<float>":
                        float x3 = jObject["X"]?.ToObject<float>() ?? 0f;
                        float y3 = jObject["Y"]?.ToObject<float>() ?? 0f;
                        float z3 = jObject["Z"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector3D<float>(x3, y3, z3);

                    case "vec4":
                    case "Vector4D<float>":
                        float x4 = jObject["X"]?.ToObject<float>() ?? 0f;
                        float y4 = jObject["Y"]?.ToObject<float>() ?? 0f;
                        float z4 = jObject["Z"]?.ToObject<float>() ?? 0f;
                        float w4 = jObject["W"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector4D<float>(x4, y4, z4, w4);

                    default:
                        return jObject.ToObject<object>();
                }
            }

            return value;
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
