using AtomEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Collections.Generic;
using AtomEngine.RenderEntity;
using System.Reflection;
using OpenglLib;
using EngineLib;

namespace Editor
{
    internal class BuildManager : IService
    {
        private SceneManager _sceneManager;
        private Window _mainWindow;
        private WindowBuildFileRouter _fileConfiguration;
        public Task InitializeAsync()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();

            return Task.CompletedTask;
        }

        public void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        public async Task<bool> BuildProject(BuildConfig config)
        {
            try
            {
                Status.SetStatus("Начало сборки проекта...");

                if (_sceneManager.CurrentScene.IsDirty)
                {
                    var result = await ConfirmationDialog.Show(
                        _mainWindow,
                        "Предупреждение",
                        "Сцена содержит несохраненные изменения. Сохранить перед сборкой?",
                        true);

                    if (result == ConfirmationDialog.DialogResult.Cancel)
                        return false;

                    if (result == ConfirmationDialog.DialogResult.Yes)
                        await _sceneManager.HandleSaveScene();
                }

                if (string.IsNullOrEmpty(config.OutputPath))
                {
                    if (string.IsNullOrWhiteSpace(config.OutputPath))
                    {
                        config.OutputPath = await FileDialogService.ChooseFolderAsync(
                            _mainWindow,
                            "Выберите папку для сборки проекта");
                    }

                    if (string.IsNullOrEmpty(config.OutputPath))
                        return false;
                }

                string buildDir = Path.Combine(config.OutputPath, config.ProjectName);
                _fileConfiguration = new(buildDir);

                await ExportSceneData(_sceneManager.CurrentScene, buildDir);

                await ExportResources(_sceneManager.CurrentScene, buildDir);

                CopyEngineLibraries(buildDir, config.TargetPlatform);

                await CreateExecutable(buildDir, config);

                Status.SetStatus($"Сборка завершена: {buildDir}");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сборке проекта: {ex.Message}");
                Status.SetStatus("Ошибка при сборке проекта");
                return false;
            }
        }

        private async Task ExportSceneData(ProjectScene scene, string buildDir)
        {
            Status.SetStatus("Export scene data...");

            string sceneData = SceneSerializer.SerializeScene(scene);
            string sceneName = Path.GetFileNameWithoutExtension(scene.ScenePath);
            string sceneFileName = $"{sceneName}.{_fileConfiguration.SceneExtension}";
            string scenesPath = Path.Combine(_fileConfiguration.ScenesPath, sceneFileName);
            await File.WriteAllTextAsync(scenesPath, sceneData);
        }

        private async Task ExportResources(ProjectScene scene, string buildDir)
        {
            Status.SetStatus($"Export resources...");

            var metadataManager = ServiceHub.Get<MetadataManager>();
            var materialManager = ServiceHub.Get<MaterialAssetManager>();
            var meshManager = ServiceHub.Get<ModelManager>();

            HashSet<string> textureGuids = new HashSet<string>();
            HashSet<string> meshGuids = new HashSet<string>();
            HashSet<string> materialGuids = new HashSet<string>();

            foreach (var world in scene.Worlds)
            {
                foreach (var entity in world.Entities)
                {
                    foreach (var componentPair in entity.Components)
                    {
                        Type componentType = componentPair.Value.GetType();

                        if (Attribute.IsDefined(componentType, typeof(GLDependableAttribute)))
                        {
                            FieldInfo[] fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                            foreach (var field in fields)
                            {
                                if (field.Name.EndsWith("GUID") && field.FieldType == typeof(string))
                                {
                                    string guidValue = (string)field.GetValue(componentPair.Value);

                                    if (string.IsNullOrEmpty(guidValue))
                                        continue;

                                    string baseFieldName = field.Name.Substring(0, field.Name.Length - 4);
                                    FieldInfo baseField = componentType.GetField(baseFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                                    if (baseField != null)
                                    {
                                        Type fieldType = baseField.FieldType;

                                        if (typeof(Texture).IsAssignableFrom(fieldType))
                                        {
                                            textureGuids.Add(guidValue);
                                        }
                                        else if (typeof(MeshBase).IsAssignableFrom(fieldType))
                                        {
                                            meshGuids.Add(guidValue);
                                        }
                                        else if (typeof(ShaderBase).IsAssignableFrom(fieldType))
                                        {
                                            materialGuids.Add(guidValue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<string, string> resourceManifest = new Dictionary<string, string>();


            foreach (var materialGuid in materialGuids)
            {
                string sourcePath = metadataManager.GetPathByGuid(materialGuid);
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                    continue;

                MaterialAsset material = materialManager.LoadMaterialAsset(sourcePath);
                if (material != null && material.TextureReferences != null)
                {
                    foreach (var textureGuid in material.TextureReferences.Values)
                    {
                        if (!string.IsNullOrEmpty(textureGuid))
                            textureGuids.Add(textureGuid);
                    }
                }

                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(_fileConfiguration.MaterialsPath, fileName);

                File.Copy(sourcePath, destPath, true);
                resourceManifest[materialGuid] = Path.Combine(_fileConfiguration.Materials, fileName);
            }

            foreach (var textureGuid in textureGuids)
            {
                string sourcePath = metadataManager.GetPathByGuid(textureGuid);
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                    continue;

                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(_fileConfiguration.TexturesPath, fileName);

                string metaFilename = fileName + ".meta";
                string sourceMetaPath = sourcePath + ".meta";
                string destMetaPath = Path.Combine(_fileConfiguration.TexturesPath, metaFilename);

                File.Copy(sourcePath, destPath, true);
                File.Copy(sourceMetaPath, destMetaPath, true);
                resourceManifest[textureGuid] = Path.Combine(_fileConfiguration.Textures, fileName);
            }

            foreach (var meshGuid in meshGuids)
            {
                string sourcePath = metadataManager.GetPathByGuid(meshGuid);
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                    continue;

                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(_fileConfiguration.ModelsPath, fileName);

                File.Copy(sourcePath, destPath, true);
                resourceManifest[meshGuid] = Path.Combine(_fileConfiguration.Models, fileName);
            }

            string manifestPath = Path.Combine(_fileConfiguration.ResourcesPath, _fileConfiguration.ResourceManifest);
            string manifestJson = JsonConvert.SerializeObject(resourceManifest, Formatting.Indented);
            await File.WriteAllTextAsync(manifestPath, manifestJson);

            Status.SetStatus($"Экспортировано ресурсов: {textureGuids.Count} текстур, {materialGuids.Count} материалов, {meshGuids.Count} мешей");
        }

        private void CopyEngineLibraries(string buildDir, BuildPlatform platform)
        {
            Status.SetStatus("Копирование библиотек движка...");
            List<string> assemblyPaths = new List<string>();

            var assemblyManager = ServiceHub.Get<EditorAssemblyManager>();
            var assamblyTypes = Enum.GetValues<TAssembly>();
            foreach(var assemblyType in assamblyTypes)
            {
                var assembly = assemblyManager.GetAssembly(assemblyType);
                assemblyPaths.Add(assembly.Location);
            }

            string targetLibsPath = _fileConfiguration.AssembliesPath;

            Directory.CreateDirectory(targetLibsPath);

            foreach(string paths in assemblyPaths)
            {
                string fileName = Path.GetFileName(paths);
                File.Copy(paths, Path.Combine(targetLibsPath, fileName), true);
            }
        }

        private async Task CreateExecutable(string buildDir, BuildConfig config)
        {
            Status.SetStatus("Создание исполняемого файла...");

            string exeName = $"{config.ProjectName}.exe";

            string exeDirectory = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<ExePathDirectory>();

            if (Directory.GetFiles(exeDirectory, "*.exe").Length == 0)
            {
                throw new FileNotFoundError("Exe template not found");
            }

            var directories = Directory.GetDirectories(exeDirectory);
            foreach (var directory in directories)
            {
                string dirName = Path.GetFileName(directory);
                string targetDir = Path.Combine(buildDir, dirName);

                Directory.CreateDirectory(targetDir);

                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    string relativePath = file.Substring(directory.Length + 1);
                    string targetFilePath = Path.Combine(targetDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
                    File.Copy(file, targetFilePath, true);
                }
            }

            var files = Directory.GetFiles(exeDirectory);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string targetPath;

                if (Path.GetExtension(file).ToLower() == ".exe")
                {
                    targetPath = Path.Combine(buildDir, exeName);
                }
                else
                {
                    targetPath = Path.Combine(buildDir, fileName);
                }

                File.Copy(file, targetPath, true);
            }


            var runtimeConfig = new
            {
                ProjectName = config.ProjectName,
                CompanyName = config.CompanyName,
                Version = config.Version,
                StartScene = _sceneManager.CurrentScene.CurrentWorldData.WorldName,
                DebugMode = config.IncludeDebugInfo
            };

            string configJson = JsonConvert.SerializeObject(runtimeConfig);
            await File.WriteAllTextAsync(Path.Combine(buildDir, "game.config"), configJson);
        }

        public async Task ShowBuildDialog()
        {
            var config = new BuildConfig
            {
                ProjectName = _sceneManager.CurrentScene.CurrentWorldData.WorldName
            };
            await BuildProject(config);
        }
    }
}
