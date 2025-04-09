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
        public static void LoadResources(GL gl, Assimp assimp, WindowBuildFileRouter router, AssemblyManager assemblyManager, RuntimeResourceManager resourceManager)
        {
            if (gl == null)
            {
                DebLogger.Debug("GL контекст не инициализирован!");
                return;
            }

            string manifestPath = Path.Combine(router.ResourcesPath, "resources.manifest");
            if (!File.Exists(manifestPath))
            {
                DebLogger.Debug($"Манифест ресурсов не найден: {manifestPath}");
                return;
            }

            try
            {
                string manifestJson = File.ReadAllText(manifestPath);
                var resourceManifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifestJson);

                if (resourceManifest == null || resourceManifest.Count == 0)
                {
                    DebLogger.Debug("Манифест ресурсов пуст или поврежден");
                    return;
                }

                var textureEntries = new Dictionary<string, string>();
                var modelEntries = new Dictionary<string, string>();
                var materialEntries = new Dictionary<string, string>();

                foreach (var entry in resourceManifest)
                {
                    string relativePath = entry.Value;

                    if (relativePath.StartsWith(router.Textures))
                        textureEntries.Add(entry.Key, relativePath);
                    else if (relativePath.StartsWith(router.Models))
                        modelEntries.Add(entry.Key, relativePath);
                    else if (relativePath.StartsWith(router.Materials))
                        materialEntries.Add(entry.Key, relativePath);
                }

                foreach (var entry in textureEntries)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(router.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        DebLogger.Debug($"Файл ресурса не найден: {fullPath}");
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
                            DebLogger.Debug($"Ошибка при чтении метаданных текстуры: {metaEx.Message}");
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
                    DebLogger.Debug($"Загружена текстура: {relativePath}");
                }
                foreach (var entry in modelEntries)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(router.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        DebLogger.Debug($"Файл ресурса не найден: {fullPath}");
                        continue;
                    }
                    try
                    {
                        //Model model = resourceManager.GetModel(guid);
                        ModelData model = resourceManager.GetModel(guid);
                        if (model == null)
                        {
                            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            var mb_model = ModelLoader.LoadModel(fullPath, assimp, false);
                            if (mb_model.IsOk())
                            {
                                model = mb_model.Unwrap();
                                resourceManager.RegisterModel(guid, model);
                            }
                            else
                            {
                                DebLogger.Debug($"Ошибка загрузки меша {relativePath}:");
                                continue;
                            }
                        }
                        DebLogger.Debug($"Загружена модель: {relativePath}");

                    }
                    catch (Exception meshEx)
                    {
                        DebLogger.Debug($"Ошибка при загрузке меша {relativePath}: {meshEx.Message}");
                    }
                }
                foreach (var entry in materialEntries)
                {
                    string guid = entry.Key;
                    string relativePath = entry.Value;
                    string fullPath = Path.Combine(router.ResourcesPath, relativePath);

                    if (!File.Exists(fullPath))
                    {
                        DebLogger.Debug($"Файл ресурса не найден: {fullPath}");
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
                                        DebLogger.Debug($"Загружен материал: {relativePath}");
                                    }
                                }
                                catch (Exception shaderEx)
                                {
                                    DebLogger.Debug($"Ошибка при создании шейдера для материала {relativePath}: {shaderEx.Message}");
                                }
                            }
                            else
                            {
                                DebLogger.Debug($"Тип шейдера не найден или не является потомком ShaderBase: {materialConfig.ShaderRepresentationTypeName}");
                            }
                        }
                    }
                    catch (Exception materialEx)
                    {
                        DebLogger.Debug($"Ошибка при загрузке материала {relativePath}: {materialEx.Message}");
                    }
                }

                DebLogger.Debug($"Загружено ресурсов: {resourceManager.TextureCount} текстур, {resourceManager.MeshCount} мешей, {resourceManager.MaterialCount} материалов");
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка при загрузке ресурсов: {ex.Message}");
            }
        }

    }


}
