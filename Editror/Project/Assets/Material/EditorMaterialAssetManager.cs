using System.Threading.Tasks;
using System.Numerics;
using System.Linq;
using AtomEngine;
using System.IO;
using EngineLib;
using OpenglLib;
using System;
using System.ComponentModel;

namespace Editor
{
    internal class EditorMaterialAssetManager : MaterialAssetManager
    {
        public override Task InitializeAsync()
        {
            return Task.Run(async () => {
                string assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                CacheAllMaterials(assetsPath);

                await base.InitializeAsync();

                eventHub.Subscribe<FileEventCreated>(FileCreateHandler);
                eventHub.Subscribe<FileEventDeleted>(FileDeletedHandler);
                eventHub.Subscribe<FileEventRenamed>(FileRenamedHandler);
                eventHub.Subscribe<FileEventChange>(FileChangeHandler);
            });
        }

        private void FileChangeHandler(FileEventChange change)
        {
            if (change == null) return;

            if (!change.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            LoadMaterial(change.FullPath);
        }

        private void FileRenamedHandler(FileEventRenamed renamed)
        {
            if (renamed == null) return;

            if (!renamed.OldFullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase) ||
        !renamed.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            if (_cacheMaterialAssets.TryGetValue(renamed.OldFullPath, out MaterialAsset material))
            {
                _cacheMaterialAssets.Remove(renamed.OldFullPath);
                _cacheMaterialAssets[renamed.FullPath] = material;
            }
            else
            {
                LoadMaterial(renamed.FullPath);
            }
        }

        private void FileDeletedHandler(FileEventDeleted deleted)
        {
            if (deleted == null) return;

            if (!deleted.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            if (_cacheMaterialAssets.TryGetValue(deleted.FullPath, out _))
            {
                _cacheMaterialAssets.Remove(deleted.FullPath);
            }
        }

        private void FileCreateHandler(FileEventCreated created)
        {
            if (created == null) return;

            if (!created.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            if (_cacheMaterialAssets.TryGetValue(created.FullPath, out _)) return;

            MaterialAsset material = GetMaterialAssetByPath(created.FullPath);
            if (material != null)
            {
                DebLogger.Info($"Новый материал загружен: {created.FullPath}");
            }
        }


        public override void AssignShaderToMaterialFromGLSL(MaterialAsset material, string filePath)
        {
            if (material == null)
            {
                DebLogger.Error("Error assign shader to material. Material asset is not exist");
                return;
            }

            if (!File.Exists(filePath))
            {
                DebLogger.Warn($"Shader file not found: {filePath}");
                return;
            }

            try
            {
                string shaderGuid = metadataManager.GetMetadataByGuid(filePath).Guid;
                if (string.IsNullOrWhiteSpace(shaderGuid))
                {
                    DebLogger.Error($"Impossible to create material from {filePath}");
                }

                var shaderModel = GlslExtractor.ExtractShaderModel(filePath);

                material.ShaderRepresentationGuid = shaderGuid;
                material.ClearContainers();

                string assetpath = ServiceHub.Get<DirectoryExplorer>().GetPath<AssetsDirectory>();
                FileEvent fileEvent = new FileEvent();
                fileEvent.FileFullPath = filePath;
                fileEvent.FileName = Path.GetFileNameWithoutExtension(filePath);
                fileEvent.FileExtension = Path.GetExtension(filePath);
                fileEvent.FilePath = filePath.Substring(assetpath.Length);
                var result = GlslCompiler.TryToCompile(fileEvent, false);

                foreach (var item in result.UniformLocations)
                {
                    material.AddContainer(new MaterialUniformDataContainer()
                    {
                        Name = item.Key,
                        Value = 0,
                        Location = item.Value
                    });
                }


                string path = GetPathFromAsset(material);
                if (!string.IsNullOrEmpty(path))
                {
                    var materialMeta = metadataManager.GetMetadata(path);
                    if (materialMeta == null || !materialMeta.Dependencies.Contains(shaderGuid))
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
                        assetDepencyManager.AddDependencyFromPathByGuid(path, shaderGuid);
                    }
                    SaveMaterialAsset(material, path);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error processing GLSL shader file: {ex.Message}");
            }
        }

        public override void AssignShaderToMaterialFromCS(MaterialAsset material, string shaderRepresentationGuid)
        {
            if (material == null)
            {
                DebLogger.Error("Error assign shader to material. Material asset is not exist");
                return;
            }

            string filePath = metadataManager.GetPathByGuid(shaderRepresentationGuid);

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
    }
}
