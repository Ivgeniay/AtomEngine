using AtomEngine;

namespace EngineLib
{
    public class EmbeddedResourceManager : IService
    {
        protected string _resourcesPath;
        protected DirectoryExplorer _directoryExplorer;
        protected HashSet<string> _extractedResources = new HashSet<string>();

        protected string[] binaryExtensions = {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".tif", ".tiff",
            ".mp3", ".wav", ".ogg", ".mp4", ".avi", ".mov", ".wmv",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".zip", ".rar", ".7z", ".tar", ".gz",
            ".exe", ".dll", ".so", ".obj", ".o", ".bin",
            ".fbx", ".dae", ".3ds", ".blend"
        };

        public string ResourcesPath => _resourcesPath;

        public virtual async Task InitializeAsync()
        {
            _directoryExplorer = ServiceHub.Get<DirectoryExplorer>();
            _resourcesPath = _directoryExplorer.GetPath<EmbeddedResourcesDirectory>();
            await ExtractEmbeddedResourcesToDirectory("embedded:Resources/Geometry/Standart", _resourcesPath, false);
            await ExtractEmbeddedResourcesToDirectory("embedded:Resources/Graphics/Materials", _resourcesPath, true);
            await ExtractEmbeddedResourcesToDirectory("embedded:Resources/Graphics/Shaders", _resourcesPath, true);
            await Task.CompletedTask;
        }

        public virtual async Task ExtractEmbeddedResourcesToDirectory(string sourcePath, string targetDirectory, bool overwrite = false)
        {
            try
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var embeddedSourcePath = sourcePath.StartsWith(FileLoader.EmbeddedPrefix)
                    ? sourcePath
                    : $"{FileLoader.EmbeddedPrefix}{sourcePath}";

                sourcePath = sourcePath.Replace(FileLoader.EmbeddedPrefix, "");

                var files = FileLoader.SearchFilesByMask(embeddedSourcePath, "*.*", true, FileSearchMode.EmbeddedOnly);
                foreach (var embeddedFilePath in files)
                {
                    try
                    {
                        string relativePath = embeddedFilePath.Substring(FileLoader.EmbeddedPrefix.Length + sourcePath.Length);
                        if (relativePath.StartsWith("/"))
                            relativePath = relativePath.Substring(1);

                        string targetFilePath = Path.Combine(targetDirectory, relativePath);
                        string targetFileDirectory = Path.GetDirectoryName(targetFilePath);

                        if (!Directory.Exists(targetFileDirectory))
                            Directory.CreateDirectory(targetFileDirectory);

                        if (FileLoader.IsExist(targetFilePath) && !overwrite)
                        {
                            _extractedResources.Add(targetFilePath);
                            continue;
                        }

                        string extension = Path.GetExtension(targetFilePath).ToLowerInvariant();
                        bool isBinaryFile = IsBinaryFileExtension(extension);

                        if (isBinaryFile)
                        {
                            byte[] binaryContent = FileLoader.LoadBinaryFile(embeddedFilePath, FileSearchMode.EmbeddedOnly);
                            await File.WriteAllBytesAsync(targetFilePath, binaryContent);
                        }
                        else
                        {
                            string content = FileLoader.LoadFile(embeddedFilePath, FileSearchMode.EmbeddedOnly);
                            await File.WriteAllTextAsync(targetFilePath, content);
                        }

                        _extractedResources.Add(targetFilePath);
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Error extracting {embeddedFilePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error during resource extraction: {ex.Message}");
            }
        }

        public bool IsResourceExtracted(string resourcePath)
        {
            string fullPath = Path.IsPathRooted(resourcePath)
                ? resourcePath
                : Path.Combine(_resourcesPath, resourcePath);

            return _extractedResources.Contains(fullPath) || File.Exists(fullPath);
        }

        private bool IsBinaryFileExtension(string extension) =>
            binaryExtensions.Contains(extension);
    }

}
