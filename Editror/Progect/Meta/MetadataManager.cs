using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    /// <summary>
    /// Менеджер метаданных для отслеживания ресурсов проекта
    /// </summary>
    public class MetadataManager
    {
        private static MetadataManager _instance;
        public static MetadataManager Instance => _instance ??= new MetadataManager();

        private Dictionary<string, AssetMetadata> _metadataCache = new();
        private Dictionary<string, string> _guidToPathMap = new();
        private Dictionary<string, MetadataType> _extensionToTypeMap = new();

        private bool _isInitialized = false;
        private string _assetsPath;
        private const string MetaExtension = ".meta";

        private MetadataManager()
        {
            _assetsPath = DirectoryExplorer.GetPath(DirectoryType.Assets);
            InitializeExtensionMappings();
        }

        /// <summary>
        /// Инициализирует соответствия расширений типам ресурсов
        /// </summary>
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
            _extensionToTypeMap[".glsl"] = MetadataType.Shader;
            _extensionToTypeMap[".hlsl"] = MetadataType.Shader;

            _extensionToTypeMap[".cs"] = MetadataType.Script;

            _extensionToTypeMap[".scene"] = MetadataType.Scene;
            _extensionToTypeMap[".prefab"] = MetadataType.Prefab;

            _extensionToTypeMap[".mat"] = MetadataType.Material;

            _extensionToTypeMap[".json"] = MetadataType.Data;
            _extensionToTypeMap[".xml"] = MetadataType.Data;
            _extensionToTypeMap[".txt"] = MetadataType.Text;
        }

        /// <summary>
        /// Инициализирует менеджер метаданных и сканирует директорию ресурсов
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

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
        }

        /// <summary>
        /// Сканирует директорию ресурсов и загружает все метафайлы
        /// </summary>
        private void ScanAssetsDirectory()
        {
            if (!Directory.Exists(_assetsPath))
            {
                Directory.CreateDirectory(_assetsPath);
                return;
            }

            var allFiles = Directory.GetFiles(_assetsPath, "*.*", SearchOption.AllDirectories)
                .Where(file => !file.EndsWith(MetaExtension))
                .ToList();

            foreach (var filePath in allFiles)
            {
                string metaFilePath = filePath + MetaExtension;
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
                            SaveMetadata(filePath, metadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Warn($"Ошибка при чтении метафайла {metaFilePath}: {ex.Message}. Создаю новый метафайл.");
                        metadata = CreateMetadata(filePath);
                        SaveMetadata(filePath, metadata);
                    }
                }
                else
                {
                    metadata = CreateMetadata(filePath);
                    SaveMetadata(filePath, metadata);
                }

                _metadataCache[filePath] = metadata;
                _guidToPathMap[metadata.Guid] = filePath;
            }

            var allMetaFiles = Directory.GetFiles(_assetsPath, "*.meta", SearchOption.AllDirectories);
            foreach (var metaFilePath in allMetaFiles)
            {
                string originalFilePath = metaFilePath.Substring(0, metaFilePath.Length - MetaExtension.Length);
                if (!File.Exists(originalFilePath))
                {
                    DebLogger.Info($"Удаление осиротевшего метафайла: {metaFilePath}");
                    File.Delete(metaFilePath);
                }
            }
        }

        /// <summary>
        /// Создает новые метаданные для указанного файла
        /// </summary>
        public AssetMetadata CreateMetadata(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            MetadataType assetType = _extensionToTypeMap.TryGetValue(extension, out var type) ? type : MetadataType.Unknown;

            AssetMetadata metadata = assetType switch
            {
                MetadataType.Texture => new TextureMetadata(),
                MetadataType.Model => new ModelMetadata(),
                MetadataType.Audio => new AudioMetadata(),
                _ => new AssetMetadata()
            };

            metadata.Guid = Guid.NewGuid().ToString();
            metadata.AssetType = assetType;
            metadata.LastModified = DateTime.UtcNow;
            metadata.Version = 1;
            metadata.ContentHash = CalculateFileHash(filePath);

            return metadata;
        }

        /// <summary>
        /// Сохраняет метаданные в файл
        /// </summary>
        public void SaveMetadata(string filePath, AssetMetadata metadata)
        {
            if (filePath == null)
                DebLogger.Error($"Error safing metadata: file path is null or invalid");

            string metaFilePath = filePath + MetaExtension;

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

        /// <summary>
        /// Обрабатывает событие изменения файла
        /// </summary>
        public void HandleFileChanged(string filePath)
        {
            if (filePath.EndsWith(MetaExtension))
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

                SaveMetadata(filePath, metadata);
                return;
            }

            // Остальная логика проверки изменений содержимого
            string currentHash = CalculateFileHash(filePath);
            if (metadata.ContentHash != currentHash)
            {
                metadata.ContentHash = currentHash;
                metadata.LastModified = DateTime.UtcNow;
                metadata.Version++;
                SaveMetadata(filePath, metadata);
            }
        }

        /// <summary>
        /// Рассчитывает хеш содержимого файла
        /// </summary>
        internal string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Получает метаданные для указанного пути к файлу
        /// </summary>
        public AssetMetadata GetMetadata(string filePath)
        {
            if (!_isInitialized)
                Initialize();

            if (_metadataCache.TryGetValue(filePath, out var metadata))
                return metadata;

            // Если файл не в кеше, попробуем загрузить его метаданные
            string metaFilePath = filePath + MetaExtension;
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

            // Если метафайл не существует или поврежден, создаем новый
            metadata = CreateMetadata(filePath);
            SaveMetadata(filePath, metadata);
            _metadataCache[filePath] = metadata;
            _guidToPathMap[metadata.Guid] = filePath;

            return metadata;
        }

        /// <summary>
        /// Получает путь к файлу по его GUID
        /// </summary>
        public string GetPathByGuid(string guid)
        {
            if (!_isInitialized)
                Initialize();

            if (_guidToPathMap.TryGetValue(guid, out var path))
                return path;

            return null;
        }

        /// <summary>
        /// Добавляет зависимость для указанного ресурса
        /// </summary>
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

        /// <summary>
        /// Удаляет зависимость для указанного ресурса
        /// </summary>
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

        /// <summary>
        /// Добавляет тег для указанного ресурса
        /// </summary>
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

        /// <summary>
        /// Удаляет тег для указанного ресурса
        /// </summary>
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

        /// <summary>
        /// Обновляет настройки импорта для указанного ресурса
        /// </summary>
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

        /// <summary>
        /// Обрабатывает событие создания нового файла
        /// </summary>
        public void HandleFileCreated(string filePath)
        {
            if (filePath.EndsWith(MetaExtension))
                return;  // Игнорируем сами метафайлы

            var metadata = CreateMetadata(filePath);
            SaveMetadata(filePath, metadata);
            _metadataCache[filePath] = metadata;
            _guidToPathMap[metadata.Guid] = filePath;

            DebLogger.Info($"Создан новый метафайл для {filePath}");
        }

        /// <summary>
        /// Обрабатывает событие удаления файла
        /// </summary>
        public void HandleFileDeleted(string filePath)
        {
            if (filePath.EndsWith(MetaExtension))
                return;  // Игнорируем сами метафайлы

            // Удаляем метафайл
            string metaFilePath = filePath + MetaExtension;
            if (File.Exists(metaFilePath))
            {
                File.Delete(metaFilePath);
            }

            // Удаляем из кэша
            if (_metadataCache.TryGetValue(filePath, out var metadata))
            {
                _metadataCache.Remove(filePath);
                _guidToPathMap.Remove(metadata.Guid);
            }

            DebLogger.Info($"Удален метафайл для {filePath}");
        }

        /// <summary>
        /// Обрабатывает событие переименования/перемещения файла
        /// </summary>
        public void HandleFileRenamed(string oldPath, string newPath)
        {
            if (oldPath.EndsWith(MetaExtension) || newPath.EndsWith(MetaExtension))
                return;  // Игнорируем сами метафайлы

            string oldMetaPath = oldPath + MetaExtension;
            string newMetaPath = newPath + MetaExtension;

            // Перемещаем метафайл
            if (File.Exists(oldMetaPath))
            {
                File.Move(oldMetaPath, newMetaPath);
            }

            // Обновляем кэш
            if (_metadataCache.TryGetValue(oldPath, out var metadata))
            {
                _metadataCache.Remove(oldPath);
                _metadataCache[newPath] = metadata;
                _guidToPathMap[metadata.Guid] = newPath;
            }

            DebLogger.Info($"Перемещен метафайл из {oldPath} в {newPath}");
        }

        /// <summary>
        /// Находит все ресурсы, зависящие от указанного ресурса
        /// </summary>
        public List<string> FindDependentAssets(string guid)
        {
            if (!_isInitialized)
                Initialize();

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

        /// <summary>
        /// Находит все ресурсы указанного типа
        /// </summary>
        public List<string> FindAssetsByType(MetadataType assetType)
        {
            if (!_isInitialized)
                Initialize();

            return _metadataCache
                .Where(kv => kv.Value.AssetType.Equals(assetType))
                .Select(kv => kv.Key)
                .ToList();
        }

        /// <summary>
        /// Находит все ресурсы с указанным тегом
        /// </summary>
        public List<string> FindAssetsByTag(string tag)
        {
            if (!_isInitialized)
                Initialize();

            return _metadataCache
                .Where(kv => kv.Value.Tags.Contains(tag))
                .Select(kv => kv.Key)
                .ToList();
        }


        internal MetadataType GetTypeByExtension(string extension)
        {
            return _extensionToTypeMap[extension];
        }
    }

}
