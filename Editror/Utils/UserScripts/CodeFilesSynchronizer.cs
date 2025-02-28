using System.Collections.Generic;
using AtomEngine;
using System.IO;
using System;
using System.Threading.Tasks;

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

        private string _projectPath;
        private string _assetsPath;
        private bool _isInitialized = false;
        private bool _isSynchronizing = false;
        AssetFileSystem _assetFileSystem;
        private readonly object _lockObject = new object();

        // Хранит пары полных путей файлов, которые сейчас синхронизируются, чтобы избежать рекурсивных вызовов
        private readonly HashSet<string> _inProcessFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Инициализирует синхронизатор файлов кода
        /// </summary>
        public Task Initialize()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                _projectPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.CSharp_Assembly);
                _assetsPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.Assets);

                _assetFileSystem = ServiceHub.Get<AssetFileSystem>();
                _assetFileSystem.AssetCreated += OnAssetCreated;
                _assetFileSystem.AssetChanged += OnAssetChanged;
                _assetFileSystem.AssetDeleted += OnAssetDeleted;
                _assetFileSystem.AssetRenamed += OnAssetRenamed;

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
            _assetFileSystem.AssetCreated -= OnAssetCreated;
            _assetFileSystem.AssetChanged -= OnAssetChanged;
            _assetFileSystem.AssetDeleted -= OnAssetDeleted;
            _assetFileSystem.AssetRenamed -= OnAssetRenamed;

            _isInitialized = false;

            DebLogger.Debug("CodeFilesSynchronizer остановлен");
        }

        /// <summary>
        /// Обработчик события создания файла в Assets
        /// </summary>
        private void OnAssetCreated(string assetPath)
        {
            if (!IsSupportedCodeFile(assetPath))
                return;

            if (IsFileInProcess(assetPath))
                return;

            try
            {
                string relativePath = GetRelativePath(assetPath, _assetsPath);
                string projectFilePath = Path.Combine(_projectPath, relativePath);

                AddToProcessing(assetPath);
                AddToProcessing(projectFilePath);

                DebLogger.Debug($"Синхронизация (создание): {assetPath} -> {projectFilePath}");

                try
                {
                    string projectFileDir = Path.GetDirectoryName(projectFilePath);
                    if (!Directory.Exists(projectFileDir))
                    {
                        Directory.CreateDirectory(projectFileDir);
                    }

                    string content = File.ReadAllText(assetPath);
                    File.WriteAllText(projectFilePath, content);

                    DebLogger.Debug($"Файл скопирован из Assets в проект: {projectFilePath}");
                }
                finally
                {
                    RemoveFromProcessing(assetPath);
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
        private void OnAssetChanged(string assetPath)
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
                    DebLogger.Debug($"Файл не существует в проекте, будет создан: {projectFilePath}");
                    OnAssetCreated(assetPath);
                    return;
                }

                AddToProcessing(assetPath);
                AddToProcessing(projectFilePath);

                DebLogger.Debug($"Синхронизация (изменение): {assetPath} -> {projectFilePath}");

                try
                {
                    string content = File.ReadAllText(assetPath);
                    File.WriteAllText(projectFilePath, content);

                    DebLogger.Debug($"Файл обновлен в проекте: {projectFilePath}");
                }
                finally
                {
                    RemoveFromProcessing(assetPath);
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
                    OnAssetCreated(newAssetPath);
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
        /// Проверяет, должен ли файл синхронизироваться (на основе расширения)
        /// </summary>
        private bool IsSupportedCodeFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string extension = Path.GetExtension(path);
            return _supportedExtensions.Contains(extension);
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
    }
}