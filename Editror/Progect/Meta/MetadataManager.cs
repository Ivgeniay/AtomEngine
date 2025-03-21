using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System.IO;
using EngineLib;
using System;

namespace Editor
{
    public delegate void RegenerateCodeEventHandler(string sourcePath, AssetMetadata metadata);

    public class MetadataManager : IService
    {
        public event RegenerateCodeEventHandler? RegenerateCodeNeeded;

        private Dictionary<string, AssetMetadata> _metadataCache = new();
        private Dictionary<string, string> _guidToPathMap = new();
        private Dictionary<string, MetadataType> _extensionToTypeMap = new();

        private bool _isInitialized = false;
        private string _assetsPath;
        public const string META_EXTENSION = ".meta";
        private EventHub eventHub;


        private void InitializeExtensionMappings()
        {
            _extensionToTypeMap[".png"] = MetadataType.Texture;
            _extensionToTypeMap[".jpg"] = MetadataType.Texture;
            _extensionToTypeMap[".jpeg"] = MetadataType.Texture;
            _extensionToTypeMap[".bmp"] = MetadataType.Texture;
            _extensionToTypeMap[".tga"] = MetadataType.Texture;
            _extensionToTypeMap[".gif"] = MetadataType.Texture;

            _extensionToTypeMap[".fbx"] = MetadataType.Model;
            _extensionToTypeMap[".obj"] = MetadataType.Model;
            _extensionToTypeMap[".blend"] = MetadataType.Model;
            _extensionToTypeMap[".3ds"] = MetadataType.Model;

            _extensionToTypeMap[".wav"] = MetadataType.Audio;
            _extensionToTypeMap[".mp3"] = MetadataType.Audio;
            _extensionToTypeMap[".ogg"] = MetadataType.Audio;

            _extensionToTypeMap[".shader"] = MetadataType.Shader;

            _extensionToTypeMap[".glsl"] = MetadataType.ShaderSource;
            _extensionToTypeMap[".hlsl"] = MetadataType.ShaderSource;

            _extensionToTypeMap[".cs"] = MetadataType.Script;

            _extensionToTypeMap[".scene"] = MetadataType.Scene;
            _extensionToTypeMap[".prefab"] = MetadataType.Prefab;

            _extensionToTypeMap[".mat"] = MetadataType.Material;

            _extensionToTypeMap[".asset"] = MetadataType.Asset;

            _extensionToTypeMap[".json"] = MetadataType.Data;
            _extensionToTypeMap[".xml"] = MetadataType.Data;
            _extensionToTypeMap[".txt"] = MetadataType.Text;
        }

        public Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            eventHub = ServiceHub.Get<EventHub>();

            return Task.Run(() =>
            {
                _assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                InitializeExtensionMappings();

                RegenerateCodeNeeded += (e, r) =>
                {
                    DebLogger.Debug($"Needed to generate: {e}");
                    DebLogger.Debug(r);
                };

                try
                {
                    DebLogger.Info("Инициализация менеджера метаданных...");
                    ScanAssetsDirectory();
                    _isInitialized = true;
                    DebLogger.Info($"Метаданные инициализированы. Найдено {_metadataCache.Count} ресурсов.");
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при инициализации метаданных: {ex.Message}");
                }
            });
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
                AssetMetadata metadata;

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

        internal void CacheMetadata(AssetMetadata metadata, string filePath, bool withInvoke = true)
        {
            SaveMetadata(filePath, metadata);
            _metadataCache[filePath] = metadata;
            _guidToPathMap[metadata.Guid] = filePath;

            if (withInvoke)
                eventHub.SendEvent<MetadataCreateEvent>(new MetadataCreateEvent
                {
                    Metadata = metadata,
                });
        }

        public AssetMetadata CreateMetadata(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            MetadataType assetType = _extensionToTypeMap.TryGetValue(extension, out var type) ? type : MetadataType.Unknown;

            AssetMetadata metadata = assetType switch
            {
                MetadataType.Texture => new TextureMetadata(),
                MetadataType.Model => new ModelMetadata(),
                MetadataType.Audio => new AudioMetadata(),
                MetadataType.ShaderSource => new ShaderSourceMetadata(),
                MetadataType.Script => new ScriptMetadata(),
                _ => new AssetMetadata()
            };

            metadata.Guid = Guid.NewGuid().ToString();
            metadata.AssetType = assetType;
            metadata.LastModified = DateTime.UtcNow;
            metadata.Version = 1;
            metadata.ContentHash = CalculateFileHash(filePath);

            if (metadata is ScriptMetadata scrMetadata && IsGeneratedCodeFile(filePath, out string sourceGuid))
            {
                scrMetadata.IsGenerated = true;
                if (!string.IsNullOrEmpty(sourceGuid))
                {
                    scrMetadata.SourceAssetGuid = sourceGuid;

                    var sourceMetadata = GetMetadataByGuid(sourceGuid);
                    if (sourceMetadata is ShaderSourceMetadata shaderSourdeMeta)
                    {
                        shaderSourdeMeta.IsGenerator = true;
                        if (!shaderSourdeMeta.GeneratedAssets.Contains(metadata.Guid))
                        {
                            var sourcePath = GetPathByGuid(sourceGuid);
                            shaderSourdeMeta.GeneratedAssets.Add(metadata.Guid);
                            SaveMetadata(sourcePath, sourceMetadata);
                            DebLogger.Debug($"Updated source asset {sourcePath} with generated asset reference {metadata.Guid}");
                        }
                    }
                    if (sourceMetadata == null)
                    {
                        scrMetadata.SourceAssetGuid = "Unknown";
                    }
                }
                DebLogger.Debug($"Identified generated file: {filePath}, SourceGuid: {sourceGuid ?? "Unknown"}");
            }
            return metadata;
        }

        public void SaveMetadata(string filePath, AssetMetadata metadata)
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
        
        public void SaveMetadata(AssetMetadata metadata)
        {
            SaveMetadata(_guidToPathMap[metadata.Guid], metadata);
        }

        public AssetMetadata LoadMetadata(string metaFilePath)
        {
            string metaJson = File.ReadAllText(metaFilePath);

            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var baseMetadata = JsonConvert.DeserializeObject<AssetMetadata>(metaJson);

                return baseMetadata.AssetType switch
                {
                    MetadataType.Texture => JsonConvert.DeserializeObject<TextureMetadata>(metaJson, settings),
                    MetadataType.Model => JsonConvert.DeserializeObject<ModelMetadata>(metaJson, settings),
                    MetadataType.Audio => JsonConvert.DeserializeObject<AudioMetadata>(metaJson, settings),
                    MetadataType.ShaderSource => JsonConvert.DeserializeObject<ShaderSourceMetadata>(metaJson, settings),
                    MetadataType.Script => JsonConvert.DeserializeObject<ScriptMetadata>(metaJson, settings),
                    //MetadataType.Material => JsonConvert.DefaultSettings<
                    _ => baseMetadata
                };

            }
            catch (JsonSerializationException)
            {
                try
                {
                    var basicData = JsonConvert.DeserializeObject<Dictionary<string, object>>(metaJson);
                    var newMetadata = new AssetMetadata();

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

                    DebLogger.Warn($"Выполнена миграция метаданных для файла {metaFilePath}, возможно изменился формат");

                    return newMetadata;
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Не удалось мигрировать метаданные: {ex.Message}. Создаются новые метаданные");
                    return new AssetMetadata
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

            MetadataType currentAssetType = _extensionToTypeMap.TryGetValue(extension, out var type) ? type : MetadataType.Unknown;

            if (metadata.AssetType != currentAssetType)
            {
                DebLogger.Info($"Тип файла изменился с {metadata.AssetType} на {currentAssetType}: {filePath}");

                string guid = metadata.Guid;
                var tags = new List<string>(metadata.Tags);

                metadata = CreateMetadata(filePath);

                metadata.Guid = guid;
                metadata.Tags = tags;
                metadata.Version += 1;
                CacheMetadata(metadata, filePath);
                return;
            }

            string currentHash = CalculateFileHash(filePath);
            if (metadata.ContentHash != currentHash)
            {
                metadata.ContentHash = currentHash;
                metadata.LastModified = DateTime.UtcNow;
                metadata.Version++;

                if (metadata.AssetType == MetadataType.ShaderSource)
                {
                    ShaderSourceMetadata scrMetadate = metadata as ShaderSourceMetadata;
                    if (scrMetadate.IsGenerator && scrMetadate.AutoGeneration)
                    {
                        RegenerateCodeNeeded?.Invoke(filePath, metadata);
                    }
                }


                SaveMetadata(filePath, metadata);
            }
        }

        internal string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        internal void HandleFileCreated(string filePath)
        {
            if (filePath.EndsWith(META_EXTENSION))
                return;

            var metadata = CreateMetadata(filePath);
            CacheMetadata(metadata, filePath);
        }

        internal void HandleFileDeleted(string filePath)
        {
            if (filePath.EndsWith(META_EXTENSION))
                return;

            var metadata = _metadataCache.TryGetValue(filePath, out var md) ? md : null;
            if (metadata is ScriptMetadata scrMetadata)
            {
                if (scrMetadata?.IsGenerated == true && !string.IsNullOrEmpty(scrMetadata.SourceAssetGuid))
                {
                    var sourcePath = GetPathByGuid(scrMetadata.SourceAssetGuid);
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        var sourceMetadata = GetMetadata(sourcePath);
                        if (sourceMetadata is ShaderSourceMetadata shSourceMetadata && shSourceMetadata.GeneratedAssets.Contains(metadata.Guid))
                        {
                            shSourceMetadata.GeneratedAssets.Remove(metadata.Guid);
                            shSourceMetadata.IsGenerator = shSourceMetadata.GeneratedAssets.Count > 0;
                            SaveMetadata(sourcePath, sourceMetadata);
                            DebLogger.Debug($"Removed generated asset reference {metadata.Guid} from source asset {sourcePath}");
                        }
                    }
                }

            }
            if (metadata is ShaderSourceMetadata shaderSourceMetadata)
            {
                if (shaderSourceMetadata?.GeneratedAssets?.Count > 0)
                {
                    var generatedAssetsToDelete = shaderSourceMetadata.GeneratedAssets.ToList();

                    foreach (var generatedGuid in generatedAssetsToDelete)
                    {
                        var generatedPath = GetPathByGuid(generatedGuid);
                        if (!string.IsNullOrEmpty(generatedPath) && File.Exists(generatedPath))
                        {
                            DebLogger.Info($"Удаление зависимого сгенерированного файла: {generatedPath}");
                            try
                            {
                                File.Delete(generatedPath);
                                var genMetaPath = generatedPath + META_EXTENSION;
                                if (File.Exists(genMetaPath))
                                {
                                    File.Delete(genMetaPath);
                                }

                                _metadataCache.Remove(generatedPath);
                                _guidToPathMap.Remove(generatedGuid);
                            }
                            catch (Exception ex)
                            {
                                DebLogger.Error($"Ошибка при удалении сгенерированного файла {generatedPath}: {ex.Message}");
                            }
                        }
                    }
                }
            }


            string metaFilePath = filePath + META_EXTENSION;
            if (File.Exists(metaFilePath))
            {
                File.Delete(metaFilePath);
            }

            if (metadata != null)
            {
                _metadataCache.Remove(filePath);
                _guidToPathMap.Remove(metadata.Guid);
            }

            DebLogger.Info($"Удален метафайл для {filePath}");
        }

        internal void HandleFileRenamed(string oldPath, string newPath)
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
            }

            DebLogger.Info($"Перемещен метафайл из {oldPath} в {newPath}");
        }

        public AssetMetadata GetMetadataByGuid(string guid) => _metadataCache.Where(e => e.Value.Guid == guid).FirstOrDefault().Value;
        public AssetMetadata GetMetadata(string filePath)
        {
            if (_metadataCache.TryGetValue(filePath, out var metadata))
                return metadata;

            string metaFilePath = filePath + META_EXTENSION;
            if (File.Exists(metaFilePath))
            {
                try
                {
                    string metaJson = File.ReadAllText(metaFilePath);
                    metadata = JsonConvert.DeserializeObject<AssetMetadata>(metaJson);
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

        public string GetPathByGuid(string guid)
        {
            if (_guidToPathMap.TryGetValue(guid, out var path))
                return path;

            return null;
        }

        public void AddDependency(string filePath, string dependencyGuid)
        {
            var metadata = GetMetadata(filePath);

            if (!metadata.Dependencies.Contains(dependencyGuid))
            {
                metadata.Dependencies.Add(dependencyGuid);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }

        public void RemoveDependency(string filePath, string dependencyGuid)
        {
            var metadata = GetMetadata(filePath);

            if (metadata.Dependencies.Contains(dependencyGuid))
            {
                metadata.Dependencies.Remove(dependencyGuid);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }

        public void AddTag(string filePath, string tag)
        {
            var metadata = GetMetadata(filePath);

            if (!metadata.Tags.Contains(tag))
            {
                metadata.Tags.Add(tag);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }

        public void RemoveTag(string filePath, string tag)
        {
            var metadata = GetMetadata(filePath);

            if (metadata.Tags.Contains(tag))
            {
                metadata.Tags.Remove(tag);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }

        public void UpdateImportSettings(string filePath, Dictionary<string, object> settings)
        {
            var metadata = GetMetadata(filePath);

            bool changed = false;
            foreach (var setting in settings)
            {
                if (!metadata.ImportSettings.ContainsKey(setting.Key) ||
                    !metadata.ImportSettings[setting.Key].Equals(setting.Value))
                {
                    metadata.ImportSettings[setting.Key] = setting.Value;
                    changed = true;
                }
            }

            if (changed)
            {
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }

        public List<string> FindDependentAssets(string guid)
        {
            var dependentAssets = new List<string>();

            foreach (var entry in _metadataCache)
            {
                if (entry.Value.Dependencies.Contains(guid))
                {
                    dependentAssets.Add(entry.Key);
                }
            }

            return dependentAssets;
        }

        public List<string> FindAssetsByType(MetadataType assetType)
        {
            return _metadataCache
                .Where(kv => kv.Value.AssetType.Equals(assetType))
                .Select(kv => kv.Key)
                .ToList();
        }
        
        public List<string> FindAssetsByTag(string tag)
        {
            return _metadataCache
                .Where(kv => kv.Value.Tags.Contains(tag))
                .Select(kv => kv.Key)
                .ToList();
        }

        internal MetadataType GetTypeByExtension(string extension)
        {
            return _extensionToTypeMap[extension];
        }

        private bool IsGeneratedCodeFile(string filePath, out string sourceGuid)
        {
            sourceGuid = null;

            if (!filePath.EndsWith(".cs"))
                return false;

            try
            {
                string[] firstLines = new string[10];
                using (var reader = new StreamReader(filePath))
                {
                    for (int i = 0; i < firstLines.Length; i++)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                            break;
                        firstLines[i] = line;
                    }
                }

                bool isAutoGenerated = false;
                foreach (var line in firstLines)
                {
                    if (!string.IsNullOrEmpty(line) && line.Contains("<auto-generated>"))
                    {
                        isAutoGenerated = true;
                        break;
                    }
                }

                if (!isAutoGenerated)
                    return false;

                foreach (var line in firstLines)
                {
                    if (line.Contains("SourceGuid:"))
                    {
                        sourceGuid = line.Split(':')[1].Trim();
                        return true;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Warn($"Error checking if file is generated code: {ex.Message}");
                return false;
            }
        }

    }

}
