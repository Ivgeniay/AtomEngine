using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtomEngine;
using System.IO;
using EngineLib;
using System;

namespace Editor
{
    public class FileSystemWatcher : IDisposable, IService
    {
        private string _assetsPath;
        private EditorMetadataManager _metadataManager;
        private System.IO.FileSystemWatcher _fileWatcher;

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

        protected EventHub eventHub;

        public event Action<FileChangedEvent>? FileChanged;
        public event Action<FileCreateEvent>? FileCreated;
        public event Action<string>? FileDeleted;
        public event Action<string, string>? FileRenamed;

        private List<FileEventCommand> _commands = new List<FileEventCommand>();

        // Дебаунсинг событий. (ОС может кидать одно и то же событие несколько раз)
        private Dictionary<string, DateTime> _lastProcessedEvents = new Dictionary<string, DateTime>();
        private readonly object _lockObject = new object();
        private const int DebounceIntervalMs = 300;

        private bool _isInitialized = false;

        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() =>
            {
                _assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                _metadataManager = ServiceHub.Get<EditorMetadataManager>();
                eventHub = ServiceHub.Get<EventHub>();

                if (!Directory.Exists(_assetsPath))
                {
                    Directory.CreateDirectory(_assetsPath);
                }
                StartFileWatcher();
                _isInitialized = true;
            });
        }

        private void StartFileWatcher()
        {
            _fileWatcher = new System.IO.FileSystemWatcher(_assetsPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                              NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Error += OnFileError;
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

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath))
                return;

            if (!ShouldProcessEvent(e.FullPath, "Created"))
            {
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

                var extension = Path.GetExtension(e.FullPath);
                var fName = Path.GetFileNameWithoutExtension(e.FullPath);
                FileCreateEvent eventData = new FileCreateEvent()
                {
                    FileExtension = extension,
                    FileName = fName,
                    FilePath = e.FullPath.Substring(0, e.FullPath.IndexOf(fName)),
                    FileFullPath = e.FullPath,
                };
                FileCreated?.Invoke(eventData);

                foreach(var command in _commands)
                {
                    if (command.Type == FileEventType.FileCreate &&
                        command.FileExtension == eventData.FileExtension)
                    {
                        command.Command.Execute(eventData);
                    }
                }

                eventHub.SendEvent<EngineLib.FileEventCreated>(new EngineLib.FileEventCreated(e.ChangeType, e.FullPath, e.Name));
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке создания файла: {ex.Message}");
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath))
                return;

            if (!ShouldProcessEvent(e.FullPath, "Deleted"))
            {
                return;
            }

            try
            {
                _metadataManager.HandleFileDeleted(e.FullPath);
                FileDeleted?.Invoke(e.FullPath);
                eventHub.SendEvent<EngineLib.FileEventDeleted>(new EngineLib.FileEventDeleted(e.ChangeType, e.FullPath, e.Name));
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке удаления файла: {ex.Message}");
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (ShouldIgnore(e.OldFullPath) || ShouldIgnore(e.FullPath))
            {
                if (!ShouldIgnore(e.FullPath))
                {
                    var metadata = _metadataManager.GetMetadata(e.FullPath);
                    if (metadata != null)
                    {
                        OnFileChanged(sender, e);
                    }
                }
                return;
            }

            if (!ShouldProcessEvent(e.FullPath, "Renamed"))
            {
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
                FileRenamed?.Invoke(e.OldFullPath, e.FullPath);
                eventHub.SendEvent<EngineLib.FileEventRenamed>(
                    new EngineLib.FileEventRenamed(e.ChangeType, e.FullPath, e.Name, e.OldName, e.OldFullPath)
                    );
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке переименования файла: {ex.Message}");
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath))
                return;

            if (!ShouldProcessEvent(e.FullPath, "Changed"))
            {
                return;
            }

            try
            {
                if (Directory.Exists(e.FullPath))
                {
                    return;
                }

                if (!File.Exists(e.FullPath))
                {
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

                var extension = Path.GetExtension(e.FullPath);
                var fName = Path.GetFileNameWithoutExtension(e.FullPath);
                FileChangedEvent eventData = new FileChangedEvent()
                {
                    FileExtension = extension,
                    FileName = fName,
                    FilePath = e.FullPath.Substring(0, e.FullPath.IndexOf(fName)),
                    FileFullPath = e.FullPath,
                };
                FileChanged?.Invoke(eventData);
                eventHub.SendEvent<EngineLib.FileEventChange>(
                    new EngineLib.FileEventChange(e.ChangeType, e.FullPath, e.Name)
                    );

                foreach (var command in _commands)
                {
                    if (command.Type == FileEventType.FileChanged &&
                        command.FileExtension == eventData.FileExtension)
                    {
                        command.Command.Execute(eventData);
                    }
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке изменения файла: {ex.Message}");
            }
        }

        public void RegisterCommand(FileEventCommand fileEventCommand)
        {
            if (!_commands.Contains(fileEventCommand) )
            {
                _commands.Add(fileEventCommand);
            }
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
