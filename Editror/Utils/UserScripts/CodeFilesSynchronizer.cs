using System.Collections.Generic;
using AtomEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using EngineLib;

namespace Editor
{
    public class CodeFilesSynchronizer : IService, IDisposable
    {
        private readonly HashSet<string> _supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", 
            ".fs", 
            ".js", 
            ".ts",
            ".rs",
            ".glsl",
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

            });
        }

        public void Dispose()
        {
            if (!_isInitialized)
                return;

            _assetFileSystem.FileCreated -= OnAssetCreated;
            _assetFileSystem.FileChanged -= OnAssetChanged;
            _assetFileSystem.FileDeleted -= OnAssetDeleted;
            _assetFileSystem.FileRenamed -= OnAssetRenamed;

            _isInitialized = false;
        }

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

                try
                {
                    string projectFileDir = Path.GetDirectoryName(projectFilePath);
                    if (!Directory.Exists(projectFileDir))
                    {
                        Directory.CreateDirectory(projectFileDir);
                    }

                    string content = File.ReadAllText(assetData.FileFullPath);
                    File.WriteAllText(projectFilePath, content);
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

                try
                {
                    string content = File.ReadAllText(assetData.FileFullPath);
                    File.WriteAllText(projectFilePath, content);
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
                    return;
                }

                AddToProcessing(assetPath);
                AddToProcessing(projectFilePath);

                try
                {
                    File.Delete(projectFilePath);
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

                try
                {
                    string assetDir = Path.GetDirectoryName(assetPath);
                    if (!Directory.Exists(assetDir))
                    {
                        Directory.CreateDirectory(assetDir);
                    }

                    string content = File.ReadAllText(projectFilePath);
                    File.WriteAllText(assetPath, content);
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
                    OnProjectFileCreated(projectFilePath);
                    return;
                }

                AddToProcessing(projectFilePath);
                AddToProcessing(assetPath);

                try
                {
                    string content = File.ReadAllText(projectFilePath);
                    File.WriteAllText(assetPath, content);
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
                    return;
                }

                AddToProcessing(projectFilePath);
                AddToProcessing(assetPath);

                try
                {
                    File.Delete(assetPath);
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
                    OnProjectFileCreated(newProjectFilePath);
                    return;
                }

                AddToProcessing(oldProjectFilePath);
                AddToProcessing(newProjectFilePath);
                AddToProcessing(oldAssetPath);
                AddToProcessing(newAssetPath);

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

        private string GetRelativePath(string fullPath, string basePath)
        {
            return Path.GetRelativePath(basePath, fullPath);
        }

        private void AddToProcessing(string path)
        {
            lock (_lockObject)
            {
                _inProcessFiles.Add(path);
            }
        }

        private void RemoveFromProcessing(string path)
        {
            lock (_lockObject)
            {
                _inProcessFiles.Remove(path);
            }
        }

        private bool IsFileInProcess(string path)
        {
            lock (_lockObject)
            {
                return _inProcessFiles.Contains(path);
            }
        }


        private bool IsInExcludedDirectory(string path, string basePath)
        {
            string relativePath = GetRelativePath(path, basePath);
            string[] pathSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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
            string basePath = path.StartsWith(_projectPath) ? _projectPath : _assetsPath;

            return IsInExcludedDirectory(path, basePath);
        }

        #region SyncFilesOnStart
        private void SynchronizeDirectories()
        {
            try
            {
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

                var commonFiles = projectPathsDict.Keys.Intersect(assetPathsDict.Keys).ToList();
                foreach (var relativePath in commonFiles)
                {
                    var projectPath = projectPathsDict[relativePath];
                    var assetPath = assetPathsDict[relativePath];

                    var syncDirection = DetermineSyncDirection(assetPath, projectPath);
                    if (syncDirection == SyncDirection.AssetToProject)
                    {
                        CopyFile(assetPath, projectPath);
                    }
                    else if (syncDirection == SyncDirection.ProjectToAsset)
                    {
                        CopyFile(projectPath, assetPath);
                    }
                }

                var assetsOnlyFiles = assetPathsDict.Keys.Except(projectPathsDict.Keys).ToList();
                foreach (var relativePath in assetsOnlyFiles)
                {
                    var assetPath = assetPathsDict[relativePath];
                    var projectPath = Path.Combine(_projectPath, relativePath);
                    CopyFile(assetPath, projectPath);
                }

                var projectOnlyFiles = projectPathsDict.Keys.Except(assetPathsDict.Keys).ToList();
                foreach (var relativePath in projectOnlyFiles)
                {
                    var projectPath = projectPathsDict[relativePath];
                    DeleteFile(projectPath);
                }

            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при синхронизации директорий: {ex.Message}");
            }
        }

        private enum SyncDirection
        {
            NoSync,
            AssetToProject,
            ProjectToAsset
        }

        private SyncDirection DetermineSyncDirection(string assetPath, string projectPath)
        {
            try
            {
                var assetInfo = new FileInfo(assetPath);
                var projectInfo = new FileInfo(projectPath);

                var timeDiff = assetInfo.LastWriteTimeUtc - projectInfo.LastWriteTimeUtc;

                if (Math.Abs(timeDiff.TotalSeconds) > 2)
                {
                    return timeDiff.TotalSeconds > 0
                        ? SyncDirection.AssetToProject
                        : SyncDirection.ProjectToAsset;
                }

                string assetHash = CalculateFileHash(assetPath);
                string projectHash = CalculateFileHash(projectPath);

                if (assetHash != projectHash)
                {
                    return assetInfo.LastWriteTimeUtc >= projectInfo.LastWriteTimeUtc
                        ? SyncDirection.AssetToProject
                        : SyncDirection.ProjectToAsset;
                }

                return SyncDirection.NoSync;
            }
            catch (Exception ex)
            {
                DebLogger.Warn($"Ошибка при определении направления синхронизации между {assetPath} и {projectPath}: {ex.Message}");
                return SyncDirection.NoSync;
            }
        }

        private void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при удалении файла {filePath}: {ex.Message}");
            }
        }

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