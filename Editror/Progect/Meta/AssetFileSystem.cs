using AtomEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Editor
{
    /// <summary>
    /// Класс для управления файловой системой ресурсов
    /// </summary>
    public class AssetFileSystem : IDisposable, IService
    {
        private string _assetsPath;
        private MetadataManager _metadataManager;
        private FileSystemWatcher _fileWatcher;

        private readonly List<string> _ignorePatterns = new()
        {
            @"\.meta$",           // Метафайлы
            @"^\.git",            // Git-директории
            @"^\.vs",             // Visual Studio директории
            @"^\.idea",           // JetBrains IDE директории
            @"^\.vscode",         // VS Code директории
            @"^Temp",             // Временные файлы
            @"^Logs",             // Логи
            @"^Library",          // Библиотеки
            @"\.tmp$",            // Временные файлы
            @"\.bak$",            // Резервные копии
            @"~$"                 // Временные файлы Office
        };

        public event Action<string> AssetChanged;
        public event Action<string> AssetCreated;
        public event Action<string> AssetDeleted;
        public event Action<string, string> AssetRenamed;

        // Дебаунсинг событий. (ОС может кидать одно и то же событие несколько раз)
        private Dictionary<string, DateTime> _lastProcessedEvents = new Dictionary<string, DateTime>();
        private readonly object _lockObject = new object();
        private const int DebounceIntervalMs = 300;

        private bool _isInitialized = false;

        /// <summary>
        /// Инициализирует файловую систему ресурсов
        /// </summary>
        public Task Initialize()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() =>
            {
                _assetsPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.Assets);
                _metadataManager = ServiceHub.Get<MetadataManager>();

                if (!Directory.Exists(_assetsPath))
                {
                    Directory.CreateDirectory(_assetsPath);
                }

                // Инициализируем менеджер метаданных
                _metadataManager.Initialize();

                // Запускаем наблюдение за файловой системой
                StartFileWatcher();

                DebLogger.Info("Файловая система ресурсов инициализирована");
                _isInitialized = true;
            });
        }

        /// <summary>
        /// Запускает наблюдение за файловой системой
        /// </summary>
        private void StartFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher(_assetsPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                              NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            // Регистрируем обработчики событий
            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Error += OnFileError;

            DebLogger.Info("Начат мониторинг директории ресурсов");
        }

        private bool ShouldProcessEvent(string path, string eventType)
        {
            string key = $"{eventType}:{path}";

            lock (_lockObject)
            {
                DateTime now = DateTime.UtcNow;
                if (_lastProcessedEvents.TryGetValue(key, out var lastTime))
                {
                    if ((now - lastTime).TotalMilliseconds < DebounceIntervalMs)
                    {
                        return false;
                    }
                }

                _lastProcessedEvents[key] = now;
                return true;
            }
        }

        /// <summary>
        /// Проверяет, должен ли файл быть проигнорирован
        /// </summary>
        private bool ShouldIgnore(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            foreach (var pattern in _ignorePatterns)
            {
                if (Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        private void OnFileError(object sender, ErrorEventArgs e)
        {
            DebLogger.Error(e);
        }

        /// <summary>
        /// Обработчик события создания файла
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath))
                return;

            if (!ShouldProcessEvent(e.FullPath, "Created"))
            {
                DebLogger.Debug($"Пропуск дублирующего события создания: {e.FullPath}");
                return;
            }

            try
            {
                bool isDirectory = Directory.Exists(e.FullPath);

                if (!isDirectory && File.Exists(e.FullPath))
                {
                    const int maxRetries = 5;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                break;
                            }
                        }
                        catch (IOException)
                        {
                            if (retry == maxRetries - 1)
                            {
                                DebLogger.Warn($"Не удалось получить доступ к файлу после {maxRetries} попыток: {e.FullPath}");
                                return;
                            }

                            System.Threading.Thread.Sleep(50 * (retry + 1));
                        }
                    }
                }

                _metadataManager.HandleFileCreated(e.FullPath);
                AssetCreated?.Invoke(e.FullPath);

                DebLogger.Info($"Создан новый ресурс: {e.FullPath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке создания файла: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события удаления файла
        /// </summary>
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath))
                return;

            if (!ShouldProcessEvent(e.FullPath, "Deleted"))
            {
                DebLogger.Debug($"Пропуск дублирующего события удаления: {e.FullPath}");
                return;
            }

            try
            {
                _metadataManager.HandleFileDeleted(e.FullPath);
                AssetDeleted?.Invoke(e.FullPath);

                DebLogger.Info($"Удален ресурс: {e.FullPath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке удаления файла: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события переименования файла
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (ShouldIgnore(e.OldFullPath) || ShouldIgnore(e.FullPath))
            {
                if (!ShouldIgnore(e.FullPath))
                {
                    var metadata = _metadataManager.GetMetadata(e.FullPath);
                    if (metadata != null)
                    {
                        DebLogger.Debug($"Обнаружено переименование из временного файла: {e.OldFullPath} -> {e.FullPath}. Обрабатываем как изменение.");
                        OnFileChanged(sender, e);
                    }
                }
                return;
            }

            if (!ShouldProcessEvent(e.FullPath, "Renamed"))
            {
                DebLogger.Debug($"Пропуск дублирующего события переименования: {e.OldFullPath} -> {e.FullPath}");
                return;
            }

            try
            {
                bool isDirectory = Directory.Exists(e.FullPath);

                if (!isDirectory && File.Exists(e.FullPath))
                {
                    const int maxRetries = 5;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                break;
                            }
                        }
                        catch (IOException)
                        {
                            if (retry == maxRetries - 1)
                            {
                                DebLogger.Warn($"Не удалось получить доступ к файлу после {maxRetries} попыток: {e.FullPath}");
                                return;
                            }

                            System.Threading.Thread.Sleep(50 * (retry + 1));
                        }
                    }
                }

                _metadataManager.HandleFileRenamed(e.OldFullPath, e.FullPath);
                AssetRenamed?.Invoke(e.OldFullPath, e.FullPath);

                DebLogger.Info($"Переименован ресурс: {e.OldFullPath} -> {e.FullPath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке переименования файла: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события изменения файла
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath))
                return;

            if (!ShouldProcessEvent(e.FullPath, "Changed"))
            {
                DebLogger.Debug($"Пропуск дублирующего события изменения: {e.FullPath}");
                return;
            }

            try
            {
                if (Directory.Exists(e.FullPath))
                {
                    DebLogger.Debug($"Событие изменения директории: {e.FullPath}");
                    return;
                }

                if (!File.Exists(e.FullPath))
                {
                    DebLogger.Debug($"Файл не существует при обработке события изменения: {e.FullPath}");
                    return;
                }

                const int maxRetries = 5;
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        if (retry == maxRetries - 1)
                        {
                            DebLogger.Warn($"Не удалось получить доступ к файлу после {maxRetries} попыток: {e.FullPath}");
                            return;
                        }

                        System.Threading.Thread.Sleep(50 * (retry + 1));
                    }
                }

                _metadataManager.HandleFileChanged(e.FullPath);
                AssetChanged?.Invoke(e.FullPath);

                DebLogger.Info($"Изменен ресурс: {e.FullPath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке изменения файла: {ex.Message}");
            }
        }

        /// <summary>
        /// Копирует файл ресурса с сохранением метаданных
        /// </summary>
        public bool CopyAsset(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    DebLogger.Error($"Исходный файл не существует: {sourcePath}");
                    return false;
                }

                var sourceMetadata = _metadataManager.GetMetadata(sourcePath);
                File.Copy(sourcePath, destinationPath, true);

                var newMetadata = new AssetMetadata
                {
                    Guid = Guid.NewGuid().ToString(),
                    AssetType = sourceMetadata.AssetType,
                    LastModified = DateTime.UtcNow,
                    Version = 1,
                    ContentHash = _metadataManager.CalculateFileHash(destinationPath),
                    ImportSettings = new Dictionary<string, object>(sourceMetadata.ImportSettings),
                    Tags = new List<string>(sourceMetadata.Tags)
                };

                _metadataManager.SaveMetadata(destinationPath, newMetadata);
                DebLogger.Info($"Скопирован ресурс: {sourcePath} -> {destinationPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при копировании ресурса: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Перемещает файл ресурса с сохранением метаданных
        /// </summary>
        public bool MoveAsset(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    DebLogger.Error($"Исходный файл не существует: {sourcePath}");
                    return false;
                }

                // Копируем метафайл
                string sourceMetaPath = sourcePath + ".meta";
                string destMetaPath = destinationPath + ".meta";

                if (File.Exists(sourceMetaPath))
                {
                    File.Copy(sourceMetaPath, destMetaPath, true);
                    File.Delete(sourceMetaPath);
                }

                // Перемещаем файл
                File.Move(sourcePath, destinationPath, true);

                // Обновляем метаданные в кэше
                _metadataManager.HandleFileRenamed(sourcePath, destinationPath);

                DebLogger.Info($"Перемещен ресурс: {sourcePath} -> {destinationPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при перемещении ресурса: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Импортирует файл извне в директорию ресурсов
        /// </summary>
        public string ImportAsset(string externalFilePath, string relativePath = null)
        {
            try
            {
                if (!File.Exists(externalFilePath))
                {
                    DebLogger.Error($"Файл для импорта не существует: {externalFilePath}");
                    return null;
                }

                // Определяем путь назначения
                string fileName = Path.GetFileName(externalFilePath);
                string destinationDir = _assetsPath;

                if (!string.IsNullOrEmpty(relativePath))
                {
                    destinationDir = Path.Combine(_assetsPath, relativePath);
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }
                }

                string destinationPath = Path.Combine(destinationDir, fileName);

                // Копируем файл
                File.Copy(externalFilePath, destinationPath, true);

                // Создаем метаданные
                var metadata = _metadataManager.CreateMetadata(destinationPath);
                _metadataManager.SaveMetadata(destinationPath, metadata);

                DebLogger.Info($"Импортирован ресурс: {externalFilePath} -> {destinationPath}");
                return destinationPath;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при импорте ресурса: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Экспортирует ресурс во внешнюю директорию
        /// </summary>
        public bool ExportAsset(string assetPath, string exportPath)
        {
            try
            {
                if (!File.Exists(assetPath))
                {
                    DebLogger.Error($"Ресурс для экспорта не существует: {assetPath}");
                    return false;
                }

                File.Copy(assetPath, exportPath, true);

                DebLogger.Info($"Экспортирован ресурс: {assetPath} -> {exportPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при экспорте ресурса: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получает относительный путь ресурса от корневой директории ресурсов
        /// </summary>
        public string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !fullPath.StartsWith(_assetsPath))
                return null;

            return fullPath.Substring(_assetsPath.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Получает полный путь ресурса по относительному пути
        /// </summary>
        public string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            return Path.Combine(_assetsPath, relativePath);
        }

        /// <summary>
        /// Определяет тип ресурса по его расширению
        /// </summary>
        public MetadataType GetAssetTypeByExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return MetadataType.Unknown;

            if (!extension.StartsWith("."))
                extension = "." + extension;

            return _metadataManager.GetTypeByExtension(extension);
        }

        /// <summary>
        /// Получает список всех ресурсов указанного типа
        /// </summary>
        public List<string> GetAssetsByType(MetadataType assetType)
        {
            return _metadataManager.FindAssetsByType(assetType);
        }

        /// <summary>
        /// Получает список всех ресурсов с указанным тегом
        /// </summary>
        public List<string> GetAssetsByTag(string tag)
        {
            return _metadataManager.FindAssetsByTag(tag);
        }

        /// <summary>
        /// Проверяет, является ли путь путем к ресурсу (находится в директории ресурсов)
        /// </summary>
        public bool IsAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.StartsWith(_assetsPath);
        }

        /// <summary>
        /// Получает путь к ресурсу по его GUID
        /// </summary>
        public string GetAssetPathByGuid(string guid)
        {
            return _metadataManager.GetPathByGuid(guid);
        }

        /// <summary>
        /// Получает GUID ресурса по его пути
        /// </summary>
        public string GetAssetGuid(string assetPath)
        {
            var metadata = _metadataManager.GetMetadata(assetPath);
            return metadata?.Guid;
        }

        /// <summary>
        /// Добавляет тег к ресурсу
        /// </summary>
        public void AddAssetTag(string assetPath, string tag)
        {
            _metadataManager.AddTag(assetPath, tag);
        }

        /// <summary>
        /// Удаляет тег у ресурса
        /// </summary>
        public void RemoveAssetTag(string assetPath, string tag)
        {
            _metadataManager.RemoveTag(assetPath, tag);
        }

        /// <summary>
        /// Обновляет настройки импорта ресурса
        /// </summary>
        public void UpdateAssetImportSettings(string assetPath, Dictionary<string, object> settings)
        {
            _metadataManager.UpdateImportSettings(assetPath, settings);
        }

        public void Dispose()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.Created -= OnFileCreated;
                _fileWatcher.Deleted -= OnFileDeleted;
                _fileWatcher.Renamed -= OnFileRenamed;
                _fileWatcher.Changed -= OnFileChanged;

                _fileWatcher.EnableRaisingEvents = false;

                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
        }
    }
}
