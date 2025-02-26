using System.Collections.Generic;
using System.Threading.Tasks;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    /// <summary>
    /// Статический класс для отслеживания файлов кода, созданных в папке проекта, и перемещения их в папку Assets
    /// </summary>
    public static class ProjectFileWatcher
    {
        private static FileSystemWatcher _watcher;
        private static string _projectPath;
        private static string _assetsPath;
        private static HashSet<string> _ignoredPaths;
        private static readonly HashSet<string> _supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", 
        ".fs", 
        ".js", 
        ".ts"  
    };
        private static readonly object _lockObject = new object();
        private static HashSet<string> _inProcessPaths = new HashSet<string>();
        private static bool _isInitialized = false;

        /// <summary>
        /// Инициализирует вотчер файлов
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _projectPath = DirectoryExplorer.GetPath(DirectoryType.CSharp_Assembly);
            _assetsPath = DirectoryExplorer.GetPath(DirectoryType.Assets);

            // Пути, которые следует игнорировать (служебные папки)
            _ignoredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(_projectPath, "bin"),
            Path.Combine(_projectPath, "obj"),
            Path.Combine(_projectPath, ".vs"),
            Path.Combine(_projectPath, "Properties")
        };

            // Создаем единый вотчер для файлов
            _watcher = new FileSystemWatcher(_projectPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            // Подписываемся только на события создания файлов
            _watcher.Created += OnFileCreated;

            _isInitialized = true;

            DebLogger.Debug($"ProjectFileWatcher запущен. Мониторинг кодовых файлов в папке проекта: {_projectPath}");
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
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _isInitialized = false;

            DebLogger.Debug("ProjectFileWatcher остановлен");
        }

        private static async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            string filePath = e.FullPath;

            // Проверяем поддерживаемое расширение файла
            string extension = Path.GetExtension(filePath);
            if (!_supportedExtensions.Contains(extension))
                return;

            // Небольшая задержка, чтобы файл был полностью записан
            await Task.Delay(500);

            try
            {
                lock (_lockObject)
                {
                    // Проверяем, не обрабатывается ли уже этот путь
                    if (_inProcessPaths.Contains(filePath))
                        return;

                    _inProcessPaths.Add(filePath);
                }

                // Проверяем, что это не служебный файл или папка
                if (ShouldIgnore(filePath))
                {
                    RemoveFromProcessing(filePath);
                    return;
                }

                DebLogger.Debug($"Обнаружен новый файл кода в папке проекта: {filePath}");

                // Пытаемся несколько раз, файл может быть заблокирован
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        if (!File.Exists(filePath))
                        {
                            RemoveFromProcessing(filePath);
                            return; // Файл мог быть удален или перемещен
                        }

                        // Определяем путь назначения в папке Assets
                        string relativePath = GetRelativePath(filePath, _projectPath);
                        string destinationPath = Path.Combine(_assetsPath, relativePath);

                        // Создаем директории, если они не существуют
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        // Проверяем наличие существующего файла
                        if (File.Exists(destinationPath))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(destinationPath);
                            string directory = Path.GetDirectoryName(destinationPath);

                            // Создаем уникальное имя
                            int counter = 1;
                            string newPath;
                            do
                            {
                                newPath = Path.Combine(directory, $"{fileName}_{counter}{extension}");
                                counter++;
                            }
                            while (File.Exists(newPath));

                            destinationPath = newPath;
                            DebLogger.Warn($"Файл {relativePath} уже существует. Сохраняем как {Path.GetFileName(destinationPath)}");
                        }

                        // Копируем содержимое файла
                        string content = File.ReadAllText(filePath);
                        File.WriteAllText(destinationPath, content);

                        // Удаляем оригинальный файл из папки проекта
                        File.Delete(filePath);

                        DebLogger.Debug($"Файл перемещен из {filePath} в {destinationPath}");
                        break;
                    }
                    catch (IOException)
                    {
                        // Файл может быть заблокирован, ждем и пробуем снова
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка при перемещении файла {filePath}: {ex.Message}");
                        break;
                    }
                }

                RemoveFromProcessing(filePath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка в OnFileCreated: {ex.Message}");
                RemoveFromProcessing(filePath);
            }
        }

        private static bool ShouldIgnore(string path)
        {
            // Проверяем, не находится ли путь внутри игнорируемых директорий
            foreach (var ignoredPath in _ignoredPaths)
            {
                if (path.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Проверяем, не является ли файл служебным файлом проекта
            string fileName = Path.GetFileName(path);

            // Игнорируем проектные и временные файлы
            bool isProjectFile = fileName.StartsWith(".", StringComparison.OrdinalIgnoreCase);

            return isProjectFile;
        }

        private static void RemoveFromProcessing(string path)
        {
            lock (_lockObject)
            {
                _inProcessPaths.Remove(path);
            }
        }

        /// <summary>
        /// Получает относительный путь, сохраняя структуру папок
        /// </summary>
        private static string GetRelativePath(string fullPath, string basePath)
        {
            // Получаем относительный путь от basePath (проект) до файла
            string relativePath = Path.GetRelativePath(basePath, fullPath);

            return relativePath;
        }
    }
}