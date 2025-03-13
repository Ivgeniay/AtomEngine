using AtomEngine;
using AtomEngine.RenderEntity;
using Newtonsoft.Json;
using OpenglLib;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using File = System.IO.File;
using Texture = OpenglLib.Texture;

namespace WindowsBuild
{
    static class Program
    {
        private static void Main(string[] args)
        {
            //Формируем пути к файлам
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            WindowBuildFileConfiguration configuration = new WindowBuildFileConfiguration(rootPath);

            //Собираем все .dll
            AssemblyManager assemblyManager = new AssemblyManager();
            assemblyManager.ScanDirectory(configuration.AssembliesPath);

            RuntimeResourceManager resourceManager = new RuntimeResourceManager();

            var options = new AppOptions() { Width = 800, Height = 600, Debug = false };
            using App app = new App(options);

            app.OnLoaded += (gl) => LoadResources(app.Gl, app.Assimp, configuration, assemblyManager, resourceManager);

            app.Run();
        }


        private static void LoadResources(GL gl, Assimp assimp, WindowBuildFileConfiguration configuration, AssemblyManager assemblyManager, RuntimeResourceManager resourceManager)
        {
            if (gl == null)
            {
                Console.WriteLine("GL контекст не инициализирован!");
                return;
            }

            string manifestPath = Path.Combine(configuration.ResourcesPath, "resources.manifest");
            if (!File.Exists(manifestPath))
            {
                Console.WriteLine($"Манифест ресурсов не найден: {manifestPath}");
                return;
            }

            try
            {
                string manifestJson = File.ReadAllText(manifestPath);
                var resourceManifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifestJson);

                if (resourceManifest == null || resourceManifest.Count == 0)
                {
                    Console.WriteLine("Манифест ресурсов пуст или поврежден");
                    return;
                }

                foreach (var entry in resourceManifest)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(configuration.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"Файл ресурса не найден: {fullPath}");
                        continue;
                    }

                    if (relativePath.StartsWith(configuration.Textures))
                    {
                        string metaPath = fullPath + ".meta";
                        TextureConfigurationModel textureConfig = null;

                        if (File.Exists(metaPath))
                        {
                            string metaJson = File.ReadAllText(metaPath);
                            try
                            {
                                var metaData = JsonConvert.DeserializeObject<TextureConfigurationModel>(metaJson,
                                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                            }
                            catch (Exception metaEx)
                            {
                                Console.WriteLine($"Ошибка при чтении метаданных текстуры: {metaEx.Message}");
                            }
                        }

                        var texture = new Texture(gl, fullPath, textureConfig?.TextureType ?? Silk.NET.Assimp.TextureType.Diffuse);

                        if (textureConfig != null)
                        {
                            var minFilter = textureConfig.GenerateMipmaps ? TextureMinFilter.NearestMipmapNearest : textureConfig.MinFilter;

                            if (textureConfig.GenerateMipmaps)
                            {
                                switch (minFilter)
                                {
                                    case TextureMinFilter.Nearest:
                                        minFilter = TextureMinFilter.NearestMipmapNearest;
                                        break;
                                    case TextureMinFilter.Linear:
                                        minFilter = TextureMinFilter.LinearMipmapNearest;
                                        break;
                                }
                            }
                            else
                            {
                                switch (minFilter)
                                {
                                    case TextureMinFilter.NearestMipmapNearest:
                                    case TextureMinFilter.NearestMipmapLinear:
                                        minFilter = TextureMinFilter.Nearest;
                                        break;
                                    case TextureMinFilter.LinearMipmapNearest:
                                    case TextureMinFilter.LinearMipmapLinear:
                                        minFilter = TextureMinFilter.Linear;
                                        break;
                                }
                            }

                            texture.Target = textureConfig.TextureTarget;
                            texture.ConfigureFromParameters(
                                wrapMode: textureConfig.WrapMode,
                                anisoLevel: textureConfig.AnisoLevel,
                                generateMipmaps: textureConfig.GenerateMipmaps,
                                compressed: textureConfig.CompressTexture,
                                compressionFormat: textureConfig.CompressionFormat,
                                maxSize: (uint)textureConfig.MaxSize,
                                minFilter: minFilter,
                                magFilter: textureConfig.MagFilter
                            );
                        }

                        resourceManager.RegisterTexture(guid, texture);
                        Console.WriteLine($"Загружена текстура: {relativePath}");
                    }
                    else if (relativePath.StartsWith(configuration.Models))
                    {
                        try
                        {
                            var result = ModelLoader.LoadModel(fullPath, gl, assimp, false);
                            if (result.IsOk())
                            {
                                var model = result.Unwrap();
                                if (model.Meshes.Count > 0)
                                {
                                    var mesh = model.Meshes[0];
                                    resourceManager.RegisterMesh(guid, mesh);
                                    Console.WriteLine($"Загружен меш: {relativePath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Ошибка загрузки меша {relativePath}:");
                            }
                        }
                        catch (Exception meshEx)
                        {
                            Console.WriteLine($"Ошибка при загрузке меша {relativePath}: {meshEx.Message}");
                        }
                    }
                    else if (relativePath.StartsWith(configuration.Materials))
                    {
                        try
                        {
                            string materialJson = File.ReadAllText(fullPath);
                            var materialConfig = JsonConvert.DeserializeObject<MaterialConfigurationModel>(materialJson);

                            if (materialConfig != null && !string.IsNullOrEmpty(materialConfig.ShaderRepresentationTypeName))
                            {
                                Type shaderType = assemblyManager.FindType(materialConfig.ShaderRepresentationTypeName, true);
                                if (shaderType == null)
                                {
                                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                    {
                                        shaderType = assembly.GetType(materialConfig.ShaderRepresentationTypeName);
                                        if (shaderType != null)
                                            break;
                                    }
                                }

                                if (shaderType != null && typeof(ShaderBase).IsAssignableFrom(shaderType))
                                {
                                    try
                                    {
                                        var constructor = shaderType.GetConstructor(new[] { typeof(GL) });
                                        if (constructor != null)
                                        {
                                            var shader = (ShaderBase)constructor.Invoke(new object[] { gl });

                                            if (materialConfig.UniformValues != null)
                                            {
                                                Dictionary<string, object> convertedValues = new Dictionary<string, object>();
                                                foreach (var pair in materialConfig.UniformValues)
                                                {
                                                    if (pair.Value != null)
                                                    {
                                                        convertedValues[pair.Key] = ConvertToTyped(pair.Value);
                                                    }
                                                }

                                                ApplyUniformValues(shader, convertedValues);
                                            }
                                            if (materialConfig.TextureReferences != null)
                                            {
                                                ApplyTextures(gl, shader, materialConfig.TextureReferences, resourceManager);
                                            }

                                            resourceManager.RegisterMaterial(guid, shader);
                                            Console.WriteLine($"Загружен материал: {relativePath}");
                                        }
                                    }
                                    catch (Exception shaderEx)
                                    {
                                        Console.WriteLine($"Ошибка при создании шейдера для материала {relativePath}: {shaderEx.Message}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Тип шейдера не найден или не является потомком ShaderBase: {materialConfig.ShaderRepresentationTypeName}");
                                }
                            }
                        }
                        catch (Exception materialEx)
                        {
                            Console.WriteLine($"Ошибка при загрузке материала {relativePath}: {materialEx.Message}");
                        }
                    }
                }

                Console.WriteLine($"Загружено ресурсов: {resourceManager.TextureCount} текстур, {resourceManager.MeshCount} мешей, {resourceManager.MaterialCount} материалов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке ресурсов: {ex.Message}");
            }
        }

        private static void ApplyUniformValues(ShaderBase shader, Dictionary<string, object> uniformValues)
        {
            if (uniformValues == null || uniformValues.Count == 0) return;

            Type shaderType = shader.GetType();
            shader.Use();

            foreach (var pair in uniformValues)
            {
                string propertyName = pair.Key;
                object value = pair.Value;

                if (value == null) continue;

                try
                {
                    var property = shaderType.GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(shader, ConvertValueToTargetType(value, property.PropertyType));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при установке свойства {propertyName}: {ex.Message}");
                }
            }
        }

        private static void ApplyTextures(GL gl, ShaderBase shader, Dictionary<string, string> textureReferences, RuntimeResourceManager resourceManager)
        {
            if (textureReferences == null || textureReferences.Count == 0) return;

            Type shaderType = shader.GetType();
            shader.Use();

            int textureUnit = 0;
            foreach (var pair in textureReferences)
            {
                string samplerName = pair.Key;
                string textureGuid = pair.Value;

                if (string.IsNullOrEmpty(textureGuid)) continue;

                try
                {
                    string methodName = $"{samplerName}_SetTexture";
                    var method = shaderType.GetMethod(methodName);

                    if (method != null)
                    {
                        var texture = resourceManager.GetTexture(textureGuid);
                        if (texture != null)
                        {
                            gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                            method.Invoke(shader, new object[] { texture });
                            textureUnit++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при установке текстуры {samplerName}: {ex.Message}");
                }
            }
        }

        private static object ConvertValueToTargetType(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                if (targetType == typeof(System.Numerics.Vector2) || targetType == typeof(Silk.NET.Maths.Vector2D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;

                    if (targetType == typeof(System.Numerics.Vector2))
                        return new System.Numerics.Vector2(x, y);
                    else
                        return new Silk.NET.Maths.Vector2D<float>(x, y);
                }
                else if (targetType == typeof(System.Numerics.Vector3) || targetType == typeof(Silk.NET.Maths.Vector3D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;

                    if (targetType == typeof(System.Numerics.Vector3))
                        return new System.Numerics.Vector3(x, y, z);
                    else
                        return new Silk.NET.Maths.Vector3D<float>(x, y, z);
                }
                else if (targetType == typeof(System.Numerics.Vector4) || targetType == typeof(Silk.NET.Maths.Vector4D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;
                    float w = jObject["W"]?.ToObject<float>() ?? 0f;

                    if (targetType == typeof(System.Numerics.Vector4))
                        return new System.Numerics.Vector4(x, y, z, w);
                    else
                        return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
                }
            }
            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                return Convert.ChangeType(value, targetType);
            }

            return value;
        }

        private static object ConvertToTyped(object value)
        {
            if (value == null)
                return null;

            if (value is Newtonsoft.Json.Linq.JObject jObj)
            {
                if (jObj["X"] != null && jObj["Y"] != null)
                {
                    if (jObj["Z"] != null)
                    {
                        if (jObj["W"] != null)
                        {
                            float x4 = jObj["X"].ToObject<float>();
                            float y4 = jObj["Y"].ToObject<float>();
                            float z4 = jObj["Z"].ToObject<float>();
                            float w4 = jObj["W"].ToObject<float>();
                            return new Silk.NET.Maths.Vector4D<float>(x4, y4, z4, w4);
                        }
                        else
                        {
                            float x3 = jObj["X"].ToObject<float>();
                            float y3 = jObj["Y"].ToObject<float>();
                            float z3 = jObj["Z"].ToObject<float>();
                            return new Silk.NET.Maths.Vector3D<float>(x3, y3, z3);
                        }
                    }
                    else
                    {
                        float x2 = jObj["X"].ToObject<float>();
                        float y2 = jObj["Y"].ToObject<float>();
                        return new Silk.NET.Maths.Vector2D<float>(x2, y2);
                    }
                }
                else if (jObj["Values"] != null)
                {
                    var values = jObj["Values"].ToObject<float[]>();
                    if (values.Length == 16)
                    {
                        return new Silk.NET.Maths.Matrix4X4<float>(
                            values[0], values[1], values[2], values[3],
                            values[4], values[5], values[6], values[7],
                            values[8], values[9], values[10], values[11],
                            values[12], values[13], values[14], values[15]
                        );
                    }
                    else if (values.Length == 9)
                    {
                        return new Silk.NET.Maths.Matrix3X3<float>(
                            values[0], values[1], values[2],
                            values[3], values[4], values[5],
                            values[6], values[7], values[8]
                        );
                    }
                    else if (values.Length == 4)
                    {
                        return new Silk.NET.Maths.Matrix2X2<float>(
                            values[0], values[1],
                            values[2], values[3]
                        );
                    }
                }

                if (jObj["$type"] != null)
                {
                    string typeName = jObj["$type"].ToString();
                    if (typeName.Contains("Vector3"))
                    {
                        float x = jObj["X"]?.ToObject<float>() ?? 0f;
                        float y = jObj["Y"]?.ToObject<float>() ?? 0f;
                        float z = jObj["Z"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector3D<float>(x, y, z);
                    }
                    else if (typeName.Contains("Vector2"))
                    {
                        float x = jObj["X"]?.ToObject<float>() ?? 0f;
                        float y = jObj["Y"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector2D<float>(x, y);
                    }
                    else if (typeName.Contains("Vector4"))
                    {
                        float x = jObj["X"]?.ToObject<float>() ?? 0f;
                        float y = jObj["Y"]?.ToObject<float>() ?? 0f;
                        float z = jObj["Z"]?.ToObject<float>() ?? 0f;
                        float w = jObj["W"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
                    }
                }

                return jObj;
            }

            return value;
        }
    }

    public class RuntimeResourceManager
    {
        private readonly Dictionary<string, Texture> _textures = new();
        private readonly Dictionary<string, MeshBase> _meshes = new();
        private readonly Dictionary<string, ShaderBase> _materials = new();

        public int TextureCount => _textures.Count;
        public int MeshCount => _meshes.Count;
        public int MaterialCount => _materials.Count;

        public void RegisterTexture(string guid, Texture texture)
        {
            _textures[guid] = texture;
        }

        public void RegisterMesh(string guid, MeshBase mesh)
        {
            _meshes[guid] = mesh;
        }

        public void RegisterMaterial(string guid, ShaderBase material)
        {
            _materials[guid] = material;
        }

        public Texture GetTexture(string guid)
        {
            return _textures.TryGetValue(guid, out var texture) ? texture : null;
        }

        public MeshBase GetMesh(string guid)
        {
            return _meshes.TryGetValue(guid, out var mesh) ? mesh : null;
        }

        public ShaderBase GetMaterial(string guid)
        {
            return _materials.TryGetValue(guid, out var material) ? material : null;
        }

        public void Dispose()
        {
            foreach (var texture in _textures.Values)
            {
                texture.Dispose();
            }
            _textures.Clear();

            foreach (var mesh in _meshes.Values)
            {
                mesh.Dispose();
            }
            _meshes.Clear();

            foreach (var material in _materials.Values)
            {
                material.Dispose();
            }
            _materials.Clear();
        }
    }
    
    
    public class TextureConfigurationModel
    {
        public bool GenerateMipmaps { get; set; } = true;
        public bool sRGB { get; set; } = true;
        public int MaxSize { get; set; } = 2048;
        public TextureMinFilter MinFilter { get; set; } = TextureMinFilter.Nearest;
        public TextureMagFilter MagFilter { get; set; } = TextureMagFilter.Linear;
        public int AnisoLevel { get; set; } = 1;
        public Silk.NET.OpenGL.TextureWrapMode WrapMode { get; set; } = Silk.NET.OpenGL.TextureWrapMode.Repeat;
        public InternalFormat CompressionFormat { get; set; } = InternalFormat.Rgba8;
        public TextureTarget TextureTarget { get; set; } = TextureTarget.Texture2D;
        public Silk.NET.Assimp.TextureType TextureType { get; set; } = Silk.NET.Assimp.TextureType.Diffuse;
        public bool CompressTexture { get; set; } = true;
        public float CompressionQuality { get; set; } = 50;
        public bool AlphaIsTransparency { get; set; } = false;

        public bool IsNormalMap { get; set; } = false;

        public bool IsSpriteSheet { get; set; } = false;
        public int SpritePixelsPerUnit { get; set; } = 100;
        public bool GenerateSpriteMesh { get; set; } = true;
    }

    public class MaterialConfigurationModel
    {
        public string ShaderRepresentationTypeName { get; set; } = string.Empty;

        public Dictionary<string, object> UniformValues { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> TextureReferences { get; set; } = new Dictionary<string, string>();
    }
}
