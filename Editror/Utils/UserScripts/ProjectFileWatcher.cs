using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    /// <summary>
    /// Статический класс для отслеживания файлов кода в папке проекта
    /// </summary>
    public static class ProjectFileWatcher
    {
        private static FileSystemWatcher _watcher;
        private static string _projectPath;
        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;

        /// <summary>
        /// Инициализирует вотчер файлов проекта
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _projectPath = DirectoryExplorer.GetPath(DirectoryType.CSharp_Assembly);

            _watcher = new FileSystemWatcher(_projectPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;

            _isInitialized = true;

            DebLogger.Debug($"ProjectFileWatcher запущен. Мониторинг папки проекта: {_projectPath}");
        }

        /// <summary>
        /// Освобождает ресурсы вотчера файлов
        /// </summary>
        public static void Dispose()
        {
            if (!_isInitialized)
                return;

            if (_watcher != null)
            {
                _watcher.Created -= OnFileCreated;
                _watcher.Changed -= OnFileChanged;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _isInitialized = false;

            DebLogger.Debug("ProjectFileWatcher остановлен");
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Directory.Exists(e.FullPath))
                    return;

                DebLogger.Debug($"Обнаружен новый файл в проекте: {e.FullPath}");
                CodeFilesSynchronizer.OnProjectFileCreated(e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке создания файла в проекте: {ex.Message}");
            }
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Directory.Exists(e.FullPath))
                    return;

                DebLogger.Debug($"Обнаружено изменение файла в проекте: {e.FullPath}");
                CodeFilesSynchronizer.OnProjectFileChanged(e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке изменения файла в проекте: {ex.Message}");
            }
        }

        private static void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                DebLogger.Debug($"Обнаружено удаление файла в проекте: {e.FullPath}");
                CodeFilesSynchronizer.OnProjectFileDeleted(e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке удаления файла в проекте: {ex.Message}");
            }
        }

        private static void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                DebLogger.Debug($"Обнаружено переименование файла в проекте: {e.OldFullPath} -> {e.FullPath}");
                CodeFilesSynchronizer.OnProjectFileRenamed(e.OldFullPath, e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке переименования файла в проекте: {ex.Message}");
            }
        }
    }
}