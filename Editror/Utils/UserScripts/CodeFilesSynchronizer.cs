using System.Collections.Generic;
using AtomEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using EngineLib;

namespace Editor
{
    /// <summary>
    /// Класс для синхронизации файлов кода между папкой проекта и папкой Assets
    /// </summary>
    public class CodeFilesSynchronizer : IService, IDisposable
    {
        private readonly HashSet<string> _supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", // C#
            ".fs", // F#
            ".js", // JavaScript
            ".ts"  // TypeScript
        };

        private readonly HashSet<string> _excludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj",
            "Generated", 
            ".vs",       
            ".git",      
            ".idea",     
            "node_modules", 
            "Temp",     
            "Library"   
        };  

        private string _projectPath;
        private string _assetsPath;
        private bool _isInitialized = false;
        private bool _isSynchronizing = false;
        FileSystemWatcher _assetFileSystem;
        private readonly object _lockObject = new object();

        // Хранит пары полных путей файлов, которые сейчас синхронизируются, чтобы избежать рекурсивных вызовов
        private readonly HashSet<string> _inProcessFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Инициализирует синхронизатор файлов кода
        /// </summary>
        public Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                _projectPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<CSharp_AssemblyDirectory>();
                _assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();

                SynchronizeDirectories();

                _assetFileSystem = ServiceHub.Get<FileSystemWatcher>();
                _assetFileSystem.FileCreated += OnAssetCreated;
                _assetFileSystem.FileChanged += OnAssetChanged;
                _assetFileSystem.FileDeleted += OnAssetDeleted;
                _assetFileSystem.FileRenamed += OnAssetRenamed;

                _isInitialized = true;

                DebLogger.Debug($"CodeFilesSynchronizer инициализирован. Синхронизация между папками: {_projectPath} и {_assetsPath}");
            });
        }

        /// <summary>
        /// Освобождает ресурсы синхронизатора файлов кода
        /// </summary>
        public void Dispose()
        {
            if (!_isInitialized)
                return;

            // Отписываемся от событий AssetFileSystem
            _assetFileSystem.FileCreated -= OnAssetCreated;
            _assetFileSystem.FileChanged -= OnAssetChanged;
            _assetFileSystem.FileDeleted -= OnAssetDeleted;
            _assetFileSystem.FileRenamed -= OnAssetRenamed;

            _isInitialized = false;

            DebLogger.Debug("CodeFilesSynchronizer остановлен");
        }

        /// <summary>
        /// Обработчик события создания файла в Assets
        /// </summary>
        private void OnAssetCreated(FileCreateEvent assetData)
        {
            if (!IsSupportedCodeFile(assetData.FileFullPath))
                return;

            if (IsFileInProcess(assetData.FileFullPath))
                return;

            try
            {
                string relativePath = GetRelativePath(assetData.FileFullPath, _assetsPath);
                string projectFilePath = Path.Combine(_projectPath, relativePath);

                AddToProcessing(assetData.FileFullPath);
                AddToProcessing(projectFilePath);

                DebLogger.Debug($"Синхронизация (создание): {assetData} -> {projectFilePath}");

                try
                {
                    string projectFileDir = Path.GetDirectoryName(projectFilePath);
                    if (!Directory.Exists(projectFileDir))
                    {
                        Directory.CreateDirectory(projectFileDir);
                    }

                    string content = File.ReadAllText(assetData.FileFullPath);
                    File.WriteAllText(projectFilePath, content);

                    DebLogger.Debug($"Файл скопирован из Assets в проект: {projectFilePath}");
                }
                finally
                {
                    RemoveFromProcessing(assetData.FileFullPath);
                    RemoveFromProcessing(projectFilePath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла (создание): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события изменения файла в Assets
        /// </summary>
        private void OnAssetChanged(FileChangedEvent assetData)
        {
            if (!IsSupportedCodeFile(assetData.FileFullPath))
                return;

            if (IsFileInProcess(assetData.FileFullPath))
                return;

            try
            {
                string relativePath = GetRelativePath(assetData.FileFullPath, _assetsPath);
                string projectFilePath = Path.Combine(_projectPath, relativePath);

                if (!File.Exists(projectFilePath))
                {
                    DebLogger.Debug($"Файл не существует в проекте, будет создан: {projectFilePath}");
                    OnAssetCreated(new FileCreateEvent()
                    {
                        FileExtension = assetData.FileExtension,
                        FileFullPath = assetData.FileFullPath,
                        FileName = assetData.FileName,
                        FilePath = assetData.FileFullPath,
                    });
                    return;
                }

                AddToProcessing(assetData.FileFullPath);
                AddToProcessing(projectFilePath);

                DebLogger.Debug($"Синхронизация (изменение): {assetData} -> {projectFilePath}");

                try
                {
                    string content = File.ReadAllText(assetData.FileFullPath);
                    File.WriteAllText(projectFilePath, content);

                    DebLogger.Debug($"Файл обновлен в проекте: {projectFilePath}");
                }
                finally
                {
                    RemoveFromProcessing(assetData.FileFullPath);
                    RemoveFromProcessing(projectFilePath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла (изменение): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события удаления файла в Assets
        /// </summary>
        private void OnAssetDeleted(string assetPath)
        {
            if (!IsSupportedCodeFile(assetPath))
                return;

            if (IsFileInProcess(assetPath))
                return;

            try
            {
                string relativePath = GetRelativePath(assetPath, _assetsPath);
                string projectFilePath = Path.Combine(_projectPath, relativePath);

                if (!File.Exists(projectFilePath))
                {
                    DebLogger.Debug($"Файл не существует в проекте, пропускаем удаление: {projectFilePath}");
                    return;
                }

                AddToProcessing(assetPath);
                AddToProcessing(projectFilePath);

                DebLogger.Debug($"Синхронизация (удаление): {assetPath} -> удаление {projectFilePath}");

                try
                {
                    File.Delete(projectFilePath);

                    DebLogger.Debug($"Файл удален из проекта: {projectFilePath}");
                }
                finally
                {
                    RemoveFromProcessing(assetPath);
                    RemoveFromProcessing(projectFilePath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла (удаление): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события переименования файла в Assets
        /// </summary>
        private void OnAssetRenamed(string oldAssetPath, string newAssetPath)
        {
            if (!IsSupportedCodeFile(oldAssetPath) && !IsSupportedCodeFile(newAssetPath))
                return;

            if (IsFileInProcess(oldAssetPath) || IsFileInProcess(newAssetPath))
                return;

            try
            {
                string oldRelativePath = GetRelativePath(oldAssetPath, _assetsPath);
                string newRelativePath = GetRelativePath(newAssetPath, _assetsPath);

                string oldProjectFilePath = Path.Combine(_projectPath, oldRelativePath);
                string newProjectFilePath = Path.Combine(_projectPath, newRelativePath);

                if (!File.Exists(oldProjectFilePath))
                {
                    DebLogger.Debug($"Файл не существует в проекте, будет создан новый: {newProjectFilePath}");
                    var extension = Path.GetExtension(newAssetPath);
                    var fName = Path.GetFileName(newAssetPath);
                    OnAssetCreated(new FileCreateEvent()
                    {
                        FileExtension = extension,
                        FileName = fName.Substring(0, fName.IndexOf(extension)),
                        FileFullPath = newAssetPath,
                        FilePath = newAssetPath.Substring(0, newAssetPath.IndexOf(fName)),
                    });
                    return;
                }

                AddToProcessing(oldAssetPath);
                AddToProcessing(newAssetPath);
                AddToProcessing(oldProjectFilePath);
                AddToProcessing(newProjectFilePath);

                DebLogger.Debug($"Синхронизация (переименование): {oldAssetPath} -> {newAssetPath}");

                try
                {
                    string newProjectFileDir = Path.GetDirectoryName(newProjectFilePath);
                    if (!Directory.Exists(newProjectFileDir))
                    {
                        Directory.CreateDirectory(newProjectFileDir);
                    }

                    if (File.Exists(newProjectFilePath))
                    {
                        File.Delete(newProjectFilePath);
                    }

                    string content = File.ReadAllText(newAssetPath);
                    File.WriteAllText(newProjectFilePath, content);

                    if (File.Exists(oldProjectFilePath))
                    {
                        File.Delete(oldProjectFilePath);
                    }

                    DebLogger.Debug($"Файл перемещен в проекте: {oldProjectFilePath} -> {newProjectFilePath}");
                }
                finally
                {
                    RemoveFromProcessing(oldAssetPath);
                    RemoveFromProcessing(newAssetPath);
                    RemoveFromProcessing(oldProjectFilePath);
                    RemoveFromProcessing(newProjectFilePath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла (переименование): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события создания файла в проекте
        /// </summary>
        public void OnProjectFileCreated(string projectFilePath)
        {
            if (!IsSupportedCodeFile(projectFilePath))
                return;

            if (IsFileInProcess(projectFilePath))
                return;

            try
            {
                string relativePath = GetRelativePath(projectFilePath, _projectPath);
                string assetPath = Path.Combine(_assetsPath, relativePath);

                AddToProcessing(projectFilePath);
                AddToProcessing(assetPath);

                DebLogger.Debug($"Синхронизация (создание в проекте): {projectFilePath} -> {assetPath}");

                try
                {
                    string assetDir = Path.GetDirectoryName(assetPath);
                    if (!Directory.Exists(assetDir))
                    {
                        Directory.CreateDirectory(assetDir);
                    }

                    string content = File.ReadAllText(projectFilePath);
                    File.WriteAllText(assetPath, content);

                    DebLogger.Debug($"Файл скопирован из проекта в Assets: {assetPath}");
                }
                finally
                {
                    RemoveFromProcessing(projectFilePath);
                    RemoveFromProcessing(assetPath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла из проекта (создание): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события изменения файла в проекте
        /// </summary>
        public void OnProjectFileChanged(string projectFilePath)
        {
            if (!IsSupportedCodeFile(projectFilePath))
                return;

            if (IsFileInProcess(projectFilePath))
                return;

            try
            {
                string relativePath = GetRelativePath(projectFilePath, _projectPath);
                string assetPath = Path.Combine(_assetsPath, relativePath);

                if (!File.Exists(assetPath))
                {
                    DebLogger.Debug($"Файл не существует в Assets, будет создан: {assetPath}");
                    OnProjectFileCreated(projectFilePath);
                    return;
                }

                AddToProcessing(projectFilePath);
                AddToProcessing(assetPath);

                DebLogger.Debug($"Синхронизация (изменение в проекте): {projectFilePath} -> {assetPath}");

                try
                {
                    string content = File.ReadAllText(projectFilePath);
                    File.WriteAllText(assetPath, content);

                    DebLogger.Debug($"Файл обновлен в Assets: {assetPath}");
                }
                finally
                {
                    RemoveFromProcessing(projectFilePath);
                    RemoveFromProcessing(assetPath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла из проекта (изменение): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события удаления файла в проекте
        /// </summary>
        public void OnProjectFileDeleted(string projectFilePath)
        {
            if (!IsSupportedCodeFile(projectFilePath))
                return;

            if (IsFileInProcess(projectFilePath))
                return;

            try
            {
                string relativePath = GetRelativePath(projectFilePath, _projectPath);
                string assetPath = Path.Combine(_assetsPath, relativePath);

                if (!File.Exists(assetPath))
                {
                    DebLogger.Debug($"Файл не существует в Assets, пропускаем удаление: {assetPath}");
                    return;
                }

                AddToProcessing(projectFilePath);
                AddToProcessing(assetPath);

                DebLogger.Debug($"Синхронизация (удаление в проекте): {projectFilePath} -> удаление {assetPath}");

                try
                {
                    File.Delete(assetPath);
                    DebLogger.Debug($"Файл удален из Assets: {assetPath}");
                }
                finally
                {
                    RemoveFromProcessing(projectFilePath);
                    RemoveFromProcessing(assetPath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла из проекта (удаление): {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик события переименования файла в проекте
        /// </summary>
        public void OnProjectFileRenamed(string oldProjectFilePath, string newProjectFilePath)
        {
            if (!IsSupportedCodeFile(oldProjectFilePath) && !IsSupportedCodeFile(newProjectFilePath))
                return;

            if (IsFileInProcess(oldProjectFilePath) || IsFileInProcess(newProjectFilePath))
                return;

            try
            {
                string oldRelativePath = GetRelativePath(oldProjectFilePath, _projectPath);
                string newRelativePath = GetRelativePath(newProjectFilePath, _projectPath);

                string oldAssetPath = Path.Combine(_assetsPath, oldRelativePath);
                string newAssetPath = Path.Combine(_assetsPath, newRelativePath);

                if (!File.Exists(oldAssetPath))
                {
                    DebLogger.Debug($"Файл не существует в Assets, будет создан новый: {newAssetPath}");
                    OnProjectFileCreated(newProjectFilePath);
                    return;
                }

                AddToProcessing(oldProjectFilePath);
                AddToProcessing(newProjectFilePath);
                AddToProcessing(oldAssetPath);
                AddToProcessing(newAssetPath);

                DebLogger.Debug($"Синхронизация (переименование в проекте): {oldProjectFilePath} -> {newProjectFilePath}");

                try
                {
                    string newAssetDir = Path.GetDirectoryName(newAssetPath);
                    if (!Directory.Exists(newAssetDir))
                    {
                        Directory.CreateDirectory(newAssetDir);
                    }

                    if (File.Exists(newAssetPath))
                    {
                        File.Delete(newAssetPath);
                    }

                    string content = File.ReadAllText(newProjectFilePath);
                    File.WriteAllText(newAssetPath, content);

                    if (File.Exists(oldAssetPath))
                    {
                        File.Delete(oldAssetPath);
                    }

                    DebLogger.Debug($"Файл перемещен в Assets: {oldAssetPath} -> {newAssetPath}");
                }
                finally
                {
                    RemoveFromProcessing(oldProjectFilePath);
                    RemoveFromProcessing(newProjectFilePath);
                    RemoveFromProcessing(oldAssetPath);
                    RemoveFromProcessing(newAssetPath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации файла из проекта (переименование): {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, должен ли файл синхронизироваться (на основе расширения и исключенных директорий)
        /// </summary>
        private bool IsSupportedCodeFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string extension = Path.GetExtension(path);

            if (!_supportedExtensions.Contains(extension))
                return false;

            string basePath = path.StartsWith(_projectPath) ? _projectPath : _assetsPath;

            return !IsInExcludedDirectory(path, basePath);
        }

        /// <summary>
        /// Получает относительный путь от базовой директории
        /// </summary>
        private string GetRelativePath(string fullPath, string basePath)
        {
            return Path.GetRelativePath(basePath, fullPath);
        }

        /// <summary>
        /// Добавляет файл в список обрабатываемых
        /// </summary>
        private void AddToProcessing(string path)
        {
            lock (_lockObject)
            {
                _inProcessFiles.Add(path);
            }
        }

        /// <summary>
        /// Удаляет файл из списка обрабатываемых
        /// </summary>
        private void RemoveFromProcessing(string path)
        {
            lock (_lockObject)
            {
                _inProcessFiles.Remove(path);
            }
        }

        /// <summary>
        /// Проверяет, находится ли файл в процессе синхронизации
        /// </summary>
        private bool IsFileInProcess(string path)
        {
            lock (_lockObject)
            {
                return _inProcessFiles.Contains(path);
            }
        }


        /// <summary>
        /// Проверяет, находится ли файл в исключаемой директории
        /// </summary>
        private bool IsInExcludedDirectory(string path, string basePath)
        {
            // Получаем относительный путь
            string relativePath = GetRelativePath(path, basePath);
            string[] pathSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Проверяем каждый сегмент пути
            foreach (var segment in pathSegments)
            {
                if (_excludedDirectories.Contains(segment))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsInExcludedDirectory(string path)
        {
            // Определяем базовую директорию
            string basePath = path.StartsWith(_projectPath) ? _projectPath : _assetsPath;

            // Используем приватный метод для проверки
            return IsInExcludedDirectory(path, basePath);
        }

        #region SyncFilesOnStart
        /// <summary>
        /// Выполняет полную синхронизацию между директориями при запуске
        /// </summary>
        private void SynchronizeDirectories()
        {
            try
            {
                DebLogger.Info("Начинаем полную синхронизацию кодовых файлов...");

                var projectFiles = GetCodeFiles(_projectPath);
                var assetFiles = GetCodeFiles(_assetsPath);

                var projectPathsDict = projectFiles.ToDictionary(
                    p => GetRelativePath(p, _projectPath),
                    p => p
                );

                var assetPathsDict = assetFiles.ToDictionary(
                    p => GetRelativePath(p, _assetsPath),
                    p => p
                );

                foreach (var relativePath in assetPathsDict.Keys)
                {
                    var assetPath = assetPathsDict[relativePath];

                    if (!projectPathsDict.TryGetValue(relativePath, out string projectPath))
                    {
                        projectPath = Path.Combine(_projectPath, relativePath);
                        DebLogger.Debug($"Создание файла в Project: {projectPath}");
                        CopyFile(assetPath, projectPath);
                    }
                    else
                    {
                        if (ShouldUpdateFile(assetPath, projectPath))
                        {
                            DebLogger.Debug($"Обновление файла в Project: {projectPath}");
                            CopyFile(assetPath, projectPath);
                        }
                    }
                }

                foreach (var relativePath in projectPathsDict.Keys)
                {
                    var projectPath = projectPathsDict[relativePath];

                    if (!assetPathsDict.TryGetValue(relativePath, out string assetPath))
                    {
                        assetPath = Path.Combine(_assetsPath, relativePath);
                        DebLogger.Debug($"Создание файла в Assets: {assetPath}");
                        CopyFile(projectPath, assetPath);
                    }
                }

                DebLogger.Info("Полная синхронизация кодовых файлов завершена");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации директорий: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает список файлов кода в указанной директории
        /// </summary>
        private List<string> GetCodeFiles(string directory)
        {
            var files = new List<string>();

            foreach (var ext in _supportedExtensions)
            {
                var allFiles = Directory.GetFiles(directory, $"*{ext}", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    if (!IsInExcludedDirectory(file, directory))
                    {
                        files.Add(file);
                    }
                }
            }

            return files;
        }

        /// <summary>
        /// Проверяет, нужно ли обновить файл (true если source новее)
        /// </summary>
        private bool ShouldUpdateFile(string sourcePath, string targetPath)
        {
            try
            {
                var sourceInfo = new FileInfo(sourcePath);
                var targetInfo = new FileInfo(targetPath);

                if (sourceInfo.LastWriteTimeUtc - targetInfo.LastWriteTimeUtc > TimeSpan.FromSeconds(2))
                {
                    return sourceInfo.LastWriteTimeUtc > targetInfo.LastWriteTimeUtc;
                }

                string sourceHash = CalculateFileHash(sourcePath);
                string targetHash = CalculateFileHash(targetPath);

                return sourceHash != targetHash;
            }
            catch (Exception ex)
            {
                DebLogger.Warn($"Ошибка при сравнении файлов {sourcePath} и {targetPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Копирует файл, создавая необходимые директории
        /// </summary>
        private void CopyFile(string sourcePath, string targetPath)
        {
            try
            {
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(sourcePath, targetPath, true);

                var sourceInfo = new FileInfo(sourcePath);
                File.SetLastWriteTimeUtc(targetPath, sourceInfo.LastWriteTimeUtc);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при копировании файла из {sourcePath} в {targetPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Рассчитывает хеш содержимого файла
        /// </summary>
        private string CalculateFileHash(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        #endregion

    }
}