using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System.IO;
using EngineLib;
using OpenglLib;
using System;

namespace Editor
{
    public class EditorMetadataManager : MetadataManager
    {
        private string _assetsPath;

        public override Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                await base.InitializeAsync();

                _assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                try
                {
                    ScanAssetsDirectory();
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при инициализации метаданных: {ex.Message}");
                }
            });
        }


        protected override FileMetadata CreateMetadata(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            MetadataType assetType = _extensionToTypeMap.TryGetValue(extension, out var type) ? type : MetadataType.Unknown;

            return CreateMetadataWithType(filePath, assetType);
        }
        protected override FileMetadata CreateMetadataWithType(string filePath, MetadataType type)
        {
            FileMetadata metadata = type switch
            {
                MetadataType.Texture => new TextureMetadata(),
                MetadataType.Model => new ModelMetadata(),
                MetadataType.Audio => new AudioMetadata(),
                MetadataType.ShaderSource => new ShaderSourceMetadata(),
                MetadataType.Script => new ScriptMetadata(),
                MetadataType.Shader => new ShaderMetadata(),
                _ => new FileMetadata()
            };

            metadata.Guid = Guid.NewGuid().ToString();
            metadata.AssetType = type;
            metadata.LastModified = DateTime.UtcNow;
            metadata.Version = 1;
            metadata.ContentHash = CalculateFileHash(filePath);
            metadata.Name = Path.GetFileNameWithoutExtension(filePath);

            eventHub.SendEvent<MetadataCreateEvent>(new MetadataCreateEvent
            {
                Metadata = metadata,
            });

            return metadata;
        }
        public override FileMetadata CreateMetadataShaderAndCache(string filePath, string shaderType)
        {
            FileMetadata meta = null;
            if (_metadataCache.TryGetValue(filePath, out var metadata))
            {
                meta = metadata;
            }
            else
            {
                meta = CreateMetadataWithTypeAndCache(filePath, MetadataType.Shader);
            }

            var shaderMeta = meta as ShaderMetadata;
            if (shaderMeta != null)
            {
                shaderMeta.ShaderType = shaderType;
                CacheMetadata(shaderMeta, filePath);
            }

            return meta;
        }
        public override FileMetadata CreateMetadataWithTypeAndCache(string filePath, MetadataType type)
        {
            var metadata = CreateMetadataWithType(filePath, type);
            metadata.IsTypeExplicitlySet = true;
            CacheMetadata(metadata, filePath);
            return metadata;
        }
        public override void SaveMetadata(string filePath, FileMetadata metadata)
        {
            if (filePath == null)
                DebLogger.Error($"Error safing metadata: file path is null or invalid");

            string metaFilePath = filePath + META_EXTENSION;

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };

            string metaJson = JsonConvert.SerializeObject(metadata, settings);
            File.WriteAllText(metaFilePath, metaJson);
        }
        public override void SaveMetadata(FileMetadata metadata)
        {
            SaveMetadata(_guidToPathMap[metadata.Guid], metadata);
        }
        public override FileMetadata LoadMetadata(string metaFilePath)
        {
            string metaJson = File.ReadAllText(metaFilePath);

            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var baseMetadata = JsonConvert.DeserializeObject<FileMetadata>(metaJson);

                return baseMetadata.AssetType switch
                {
                    MetadataType.Texture => JsonConvert.DeserializeObject<TextureMetadata>(metaJson, settings),
                    MetadataType.Model => JsonConvert.DeserializeObject<ModelMetadata>(metaJson, settings),
                    MetadataType.Audio => JsonConvert.DeserializeObject<AudioMetadata>(metaJson, settings),
                    MetadataType.ShaderSource => JsonConvert.DeserializeObject<ShaderSourceMetadata>(metaJson, settings),
                    MetadataType.Script => JsonConvert.DeserializeObject<ScriptMetadata>(metaJson, settings),
                    MetadataType.Shader => JsonConvert.DeserializeObject<ShaderMetadata>(metaJson, settings),
                    //MetadataType.Material => JsonConvert.DefaultSettings<
                    _ => baseMetadata
                };

            }
            catch (JsonSerializationException)
            {
                try
                {
                    var basicData = JsonConvert.DeserializeObject<Dictionary<string, object>>(metaJson);
                    var newMetadata = new FileMetadata();

                    if (basicData.TryGetValue("Guid", out var guidObj) && guidObj is string guid)
                        newMetadata.Guid = guid;
                    else
                        newMetadata.Guid = Guid.NewGuid().ToString();

                    if (basicData.TryGetValue("AssetType", out var typeObj))
                    {
                        if (typeObj is string typeStr && Enum.TryParse<MetadataType>(typeStr, out var type))
                            newMetadata.AssetType = type;
                        else if (typeObj is long typeNum)
                            newMetadata.AssetType = (MetadataType)typeNum;
                    }

                    newMetadata.Version = 1;
                    if (basicData.TryGetValue("Version", out var versionObj) && versionObj is long ver)
                        newMetadata.Version = (int)ver + 1;

                    return newMetadata;
                }
                catch (Exception ex)
                {
                    return new FileMetadata
                    {
                        Guid = Guid.NewGuid().ToString(),
                        Version = 1
                    };
                }
            }
        }

        public void HandleFileChanged(string filePath)
        {
            if (filePath.EndsWith(META_EXTENSION))
                return;

            var metadata = GetMetadata(filePath);
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (!metadata.IsTypeExplicitlySet)
            {
                MetadataType currentAssetType = _extensionToTypeMap.TryGetValue(extension, out var type) ? type : MetadataType.Unknown;

                if (metadata.AssetType != currentAssetType)
                {
                    string guid = metadata.Guid;
                    var tags = new List<string>(metadata.Tags);

                    metadata = CreateMetadata(filePath);

                    metadata.Guid = guid;
                    metadata.Tags = tags;
                    metadata.Version += 1;
                    CacheMetadata(metadata, filePath);
                    return;
                }
            }

            string currentHash = CalculateFileHash(filePath);
            if (metadata.ContentHash != currentHash)
            {
                metadata.ContentHash = currentHash;
                metadata.LastModified = DateTime.UtcNow;
                metadata.Version++;
                SaveMetadata(filePath, metadata);

                eventHub.SendEvent<MetadataChandedEvent>(new MetadataChandedEvent
                {
                    Metadata = metadata,
                });
            }
        }
        public void HandleFileCreated(string filePath)
        {
            if (filePath.EndsWith(META_EXTENSION))
                return;

            string metaFilePath = filePath + META_EXTENSION;
            if (File.Exists(metaFilePath))
            {
                var metadata = LoadMetadata(metaFilePath);
                CacheMetadata(metadata, filePath);
            }
            else
            {
                var metadata = CreateMetadata(filePath);
                CacheMetadata(metadata, filePath);
            }
        }
        public void HandleFileDeleted(string filePath)
        {
            if (filePath.EndsWith(META_EXTENSION))
                return;

            var metadata = _metadataCache.TryGetValue(filePath, out var md) ? md : null;

            string metaFilePath = filePath + META_EXTENSION;
            if (File.Exists(metaFilePath))
            {
                File.Delete(metaFilePath);
            }

            if (metadata != null)
            {
                eventHub.SendEvent<MetadataDeletedEvent>(new MetadataDeletedEvent
                {
                    Metadata = metadata,
                });
                _metadataCache.Remove(filePath);
                _guidToPathMap.Remove(metadata.Guid);
            }
        }
        public void HandleFileRenamed(string oldPath, string newPath)
        {
            if (oldPath.EndsWith(META_EXTENSION) || newPath.EndsWith(META_EXTENSION))
                return;

            string oldMetaPath = oldPath + META_EXTENSION;
            string newMetaPath = newPath + META_EXTENSION;

            if (File.Exists(oldMetaPath))
            {
                File.Move(oldMetaPath, newMetaPath);
            }

            if (_metadataCache.TryGetValue(oldPath, out var metadata))
            {
                _metadataCache.Remove(oldPath);
                _metadataCache[newPath] = metadata;
                _guidToPathMap[metadata.Guid] = newPath;
                metadata.Name = Path.GetFileNameWithoutExtension(newPath);
                CacheMetadata(metadata, newPath);
            }

            eventHub.SendEvent<MetadataChandedEvent>(new MetadataChandedEvent
            {
                Metadata = metadata,
            });
        }

        public override FileMetadata GetMetadataByName(string name) => _metadataCache.Where(e => e.Value.Name == name).FirstOrDefault().Value;
        public override FileMetadata GetMetadataByGuid(string guid) => _metadataCache.Where(e => e.Value.Guid == guid).FirstOrDefault().Value;
        public override FileMetadata GetMetadata(string filePath)
        {
            if (_metadataCache.TryGetValue(filePath, out var metadata))
                return metadata;

            string metaFilePath = filePath + META_EXTENSION;
            if (File.Exists(metaFilePath))
            {
                try
                {
                    string metaJson = File.ReadAllText(metaFilePath);
                    metadata = JsonConvert.DeserializeObject<FileMetadata>(metaJson);
                    _metadataCache[filePath] = metadata;
                    _guidToPathMap[metadata.Guid] = filePath;
                    return metadata;
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при чтении метафайла {metaFilePath}: {ex.Message}");
                }
            }

            metadata = CreateMetadata(filePath);
            CacheMetadata(metadata, filePath);

            return metadata;
        }
        public override string GetPathByGuid(string guid)
        {
            if (_guidToPathMap.TryGetValue(guid, out var path))
                return path;

            return null;
        }


        private void ScanAssetsDirectory()
        {
            if (!Directory.Exists(_assetsPath))
            {
                Directory.CreateDirectory(_assetsPath);
                return;
            }

            var allFiles = Directory.GetFiles(_assetsPath, "*.*", SearchOption.AllDirectories)
                .Where(file => !file.EndsWith(META_EXTENSION))
                .ToList();

            foreach (var filePath in allFiles)
            {
                string metaFilePath = filePath + META_EXTENSION;
                FileMetadata metadata;

                if (File.Exists(metaFilePath))
                {
                    try
                    {
                        metadata = LoadMetadata(metaFilePath);
                        string currentHash = CalculateFileHash(filePath);
                        if (metadata.ContentHash != currentHash)
                        {
                            metadata.ContentHash = currentHash;
                            metadata.LastModified = DateTime.UtcNow;
                            metadata.Version++;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Warn($"Ошибка при чтении метафайла {metaFilePath}: {ex.Message}. Создание нового метафайла.");
                        metadata = CreateMetadata(filePath);
                    }
                }
                else
                {
                    metadata = CreateMetadata(filePath);
                    CacheMetadata(metadata, filePath);
                }

                SaveMetadata(filePath, metadata);
                _metadataCache[filePath] = metadata;
                _guidToPathMap[metadata.Guid] = filePath;
            }

            var allMetaFiles = Directory.GetFiles(_assetsPath, "*.meta", SearchOption.AllDirectories);
            foreach (var metaFilePath in allMetaFiles)
            {
                string originalFilePath = metaFilePath.Substring(0, metaFilePath.Length - META_EXTENSION.Length);
                if (!File.Exists(originalFilePath))
                {
                    DebLogger.Info($"Удаление осиротевшего метафайла: {metaFilePath}");
                    File.Delete(metaFilePath);
                }
            }
        }
    }

}
