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
    public static class ResourceLoader
    {
        public static void LoadResources(GL gl, Assimp assimp, WindowBuildFileRouter configuration, AssemblyManager assemblyManager, RuntimeResourceManager resourceManager)
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

                var textureEntries = new Dictionary<string, string>();
                var modelEntries = new Dictionary<string, string>();
                var materialEntries = new Dictionary<string, string>();

                foreach (var entry in resourceManifest)
                {
                    string relativePath = entry.Value;

                    if (relativePath.StartsWith(configuration.Textures))
                        textureEntries.Add(entry.Key, relativePath);
                    else if (relativePath.StartsWith(configuration.Models))
                        modelEntries.Add(entry.Key, relativePath);
                    else if (relativePath.StartsWith(configuration.Materials))
                        materialEntries.Add(entry.Key, relativePath);
                }

                foreach (var entry in textureEntries)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(configuration.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"Файл ресурса не найден: {fullPath}");
                        continue;
                    }

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
                foreach (var entry in modelEntries)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(configuration.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"Файл ресурса не найден: {fullPath}");
                        continue;
                    }
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
                foreach (var entry in materialEntries)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(configuration.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"Файл ресурса не найден: {fullPath}");
                        continue;
                    }
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
                                                    convertedValues[pair.Key] = TypeConverters.ConvertToTyped(pair.Value);
                                                }
                                            }

                                            ShaderUtils.ApplyUniformValues(shader, convertedValues);
                                        }
                                        if (materialConfig.TextureReferences != null)
                                        {
                                            ShaderUtils.ApplyTextures(gl, shader, materialConfig.TextureReferences, resourceManager);
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

                Console.WriteLine($"Загружено ресурсов: {resourceManager.TextureCount} текстур, {resourceManager.MeshCount} мешей, {resourceManager.MaterialCount} материалов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке ресурсов: {ex.Message}");
            }
        }

    }


}
