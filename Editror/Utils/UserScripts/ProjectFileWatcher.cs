using AtomEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using EngineLib;

namespace Editor
{
    public class ProjectFileWatcher : IService, IDisposable
    { 
        private System.IO.FileSystemWatcher _watcher;
        private string _projectPath;
        private readonly object _lockObject = new object();
        private bool _isInitialized = false;
        CodeFilesSynchronizer _synchronizer;

        public Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                _projectPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<CSharp_AssemblyDirectory>();

                _watcher = new System.IO.FileSystemWatcher(_projectPath)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                _synchronizer = ServiceHub.Get<CodeFilesSynchronizer>();

                _watcher.Created += OnFileCreated;
                _watcher.Changed += OnFileChanged;
                _watcher.Deleted += OnFileDeleted;
                _watcher.Renamed += OnFileRenamed;

                _isInitialized = true;

                DebLogger.Debug($"ProjectFileWatcher запущен. Мониторинг папки проекта: {_projectPath}");
            });
        }

        public void Dispose()
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

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Directory.Exists(e.FullPath))
                    return;

                if (_synchronizer.IsInExcludedDirectory(e.FullPath))
                    return;

                _synchronizer.OnProjectFileCreated(e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке создания файла в проекте: {ex.Message}");
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Directory.Exists(e.FullPath))
                    return;

                if (_synchronizer.IsInExcludedDirectory(e.FullPath))
                    return;

                _synchronizer.OnProjectFileChanged(e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке изменения файла в проекте: {ex.Message}");
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (_synchronizer.IsInExcludedDirectory(e.FullPath))
                    return;

                _synchronizer.OnProjectFileDeleted(e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке удаления файла в проекте: {ex.Message}");
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                if (_synchronizer.IsInExcludedDirectory(e.OldFullPath) || _synchronizer.IsInExcludedDirectory(e.FullPath))
                    return;

                _synchronizer.OnProjectFileRenamed(e.OldFullPath, e.FullPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке переименования файла в проекте: {ex.Message}");
            }
        }
    }
}