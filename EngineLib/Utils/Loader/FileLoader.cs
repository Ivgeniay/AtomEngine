using AtomEngine;
using System.Reflection;

namespace EngineLib
{
    public static class FileLoader
    {
        private static readonly List<IContentProvider> ContentProviders = new List<IContentProvider>();
        public const string EmbeddedPrefix = "embedded:";

        public static void RegisterContentProvider(IContentProvider provider)
        {
            if (!ContentProviders.Contains(provider)) ContentProviders.Add(provider);
        }

        public static string LoadFile(string path, FileSearchMode searchMode = FileSearchMode.BothSearch)
        {
            string content = null;
            Exception lastException = null;

            bool isEmbeddedPath = path.StartsWith(EmbeddedPrefix);
            string normalPath = isEmbeddedPath ? path.Substring(EmbeddedPrefix.Length) : path;

            if (!isEmbeddedPath && (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch))
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null)
                {
                    try
                    {
                        content = fileProvider.GetContent(path);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }
            }

            if (content == null &&
                (isEmbeddedPath || searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch))
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    try
                    {
                        string embeddedPath = isEmbeddedPath ? path : $"{EmbeddedPrefix}{normalPath}";
                        content = embeddedProvider.GetContent(embeddedPath);
                    }
                    catch (Exception ex)
                    {
                        if (lastException == null)
                            lastException = ex;
                    }
                }
            }

            if (content == null)
            {
                if (lastException != null)
                    throw new FileNotFoundError($"Error loading file: '{path}': {lastException.Message}, {lastException}");
                else
                    throw new FileNotFoundError($"File not found: {path}");
            }

            return content;
        }

        public static byte[] LoadBinaryFile(string path, FileSearchMode searchMode = FileSearchMode.BothSearch)
        {
            byte[] content = null;
            Exception lastException = null;

            bool isEmbeddedPath = path.StartsWith(EmbeddedPrefix);
            string normalPath = isEmbeddedPath ? path.Substring(EmbeddedPrefix.Length) : path;

            if (!isEmbeddedPath && (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch))
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null)
                {
                    try
                    {
                        content = fileProvider.GetBinaryContent(path);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }
            }

            if (content == null &&
                (isEmbeddedPath || searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch))
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    try
                    {
                        string embeddedPath = isEmbeddedPath ? path : $"{EmbeddedPrefix}{normalPath}";
                        content = embeddedProvider.GetBinaryContent(embeddedPath);
                    }
                    catch (Exception ex)
                    {
                        if (lastException == null)
                            lastException = ex;
                    }
                }
            }

            if (content == null)
            {
                if (lastException != null)
                    throw new FileNotFoundError($"Error loading binary file: '{path}': {lastException.Message}");
                else
                    throw new FileNotFoundError($"Binary file not found: {path}");
            }

            return content;
        }

        public static string ResolvePath(string basePath, string includePath, FileSearchMode searchMode = FileSearchMode.BothSearch)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(includePath))
                throw new ArgumentException("Base path and include path cannot be null or empty");

            bool isBaseEmbedded = basePath.StartsWith(EmbeddedPrefix);
            bool isIncludeEmbedded = includePath.StartsWith(EmbeddedPrefix);

            if (isIncludeEmbedded)
            {
                searchMode = FileSearchMode.EmbeddedOnly;
            }

            if (!isBaseEmbedded && (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch))
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null)
                {
                    try
                    {
                        string normalIncludePath = isIncludeEmbedded ? includePath.Substring(EmbeddedPrefix.Length) : includePath;
                        string resolvedPath = fileProvider.ResolvePath(basePath, normalIncludePath);

                        if (File.Exists(resolvedPath))
                            return resolvedPath;
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Debug($"Failed to resolve path in file system: {ex.Message}");
                    }
                }
            }

            if (searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch)
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    try
                    {
                        string embeddedBasePath = isBaseEmbedded ? basePath : $"{EmbeddedPrefix}{basePath}";
                        string embeddedIncludePath = isIncludeEmbedded ? includePath : $"{EmbeddedPrefix}{includePath}";

                        string resolvedPath = embeddedProvider.ResolvePath(embeddedBasePath, embeddedIncludePath);

                        try
                        {
                            embeddedProvider.GetContent(resolvedPath);
                            return resolvedPath;
                        }
                        catch
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Debug($"Failed to resolve path in embedded resources: {ex.Message}");
                    }
                }
            }

            throw new FileNotFoundError($"Could not resolve path for: {includePath} relative to {basePath}");
        }

        public static List<string> SearchFilesByMask(string path, string mask, bool depthSearch, FileSearchMode searchMode = FileSearchMode.BothSearch)
        {
            var result = new List<string>();
            Exception lastException = null;

            bool isEmbeddedPath = path.StartsWith(EmbeddedPrefix);
            string normalPath = isEmbeddedPath ? path.Substring(EmbeddedPrefix.Length) : path;

            if (!isEmbeddedPath && (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch))
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null)
                {
                    try
                    {
                        var files = fileProvider.SearchFilesByMask(path, mask, depthSearch);
                        result.AddRange(files);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        DebLogger.Debug($"Error searching files in filesystem: {ex.Message}");
                    }
                }
            }

            if (isEmbeddedPath || searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch)
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    try
                    {
                        string embeddedPath = isEmbeddedPath ? path : $"{EmbeddedPrefix}{normalPath}";
                        var files = embeddedProvider.SearchFilesByMask(embeddedPath, mask, depthSearch);
                        result.AddRange(files);
                    }
                    catch (Exception ex)
                    {
                        if (lastException == null)
                            lastException = ex;
                        DebLogger.Debug($"Error searching files in embedded resources: {ex.Message}");
                    }
                }
            }

            if (result.Count == 0 && lastException != null)
                throw new FileNotFoundError($"Error searching files by mask '{mask}' in path '{path}': {lastException.Message}");

            return result;
        }

        public static bool IsExist(string path, FileSearchMode searchMode = FileSearchMode.BothSearch)
        {
            bool isEmbeddedPath = path.StartsWith(EmbeddedPrefix);
            string normalPath = isEmbeddedPath ? path.Substring(EmbeddedPrefix.Length) : path;

            if (!isEmbeddedPath && (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch))
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null)
                {
                    try
                    {
                        if (fileProvider.IsExist(path))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Debug($"Error checking file existence in filesystem: {ex.Message}");
                    }
                }
            }

            if (isEmbeddedPath || searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch)
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    try
                    {
                        string embeddedPath = isEmbeddedPath ? path : $"{EmbeddedPrefix}{normalPath}";
                        if (embeddedProvider.IsExist(embeddedPath))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Debug($"Error checking file existence in embedded resources: {ex.Message}");
                    }
                }
            }

            return false;
        }
    }

    public enum FileSearchMode
    {
        FileSystemOnly,
        EmbeddedOnly,
        BothSearch
    }

    public interface IContentProvider
    {
        bool CanProvideContent(string path);
        string GetContent(string path);
        string ResolvePath(string basePath, string includePath);
        List<string> SearchFilesByMask(string path, string mask, bool depthSearch);
        bool IsExist(string path);
        byte[] GetBinaryContent(string path);
    }

    public class FileSystemContentProvider : IContentProvider
    {
        public bool CanProvideContent(string path)
        {
            return !path.StartsWith(FileLoader.EmbeddedPrefix);
        }

        public string GetContent(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundError($"Include file not found: {path}");

            return File.ReadAllText(path);
        }

        public string ResolvePath(string basePath, string includePath)
        {
            var sourceDir = Path.GetDirectoryName(basePath);
            return Path.GetFullPath(Path.Combine(sourceDir, includePath));
        }

        public List<string> SearchFilesByMask(string path, string mask, bool depthSearch)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            var result = new List<string>();

            string pattern = mask.Replace(".", "\\.").Replace("*", ".*").Replace("?", ".");
            var regex = new System.Text.RegularExpressions.Regex($"^{pattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var file in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(file);
                if (regex.IsMatch(fileName))
                {
                    result.Add(file);
                }
            }

            if (depthSearch)
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    try
                    {
                        var subDirFiles = SearchFilesByMask(directory, mask, true);
                        result.AddRange(subDirFiles);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }

            return result;
        }

        public byte[] GetBinaryContent(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundError($"Binary file not found: {path}");

            return File.ReadAllBytes(path);
        }

        public bool IsExist(string path)
        {
            return File.Exists(path);
        }
    }

    public class EmbeddedContentProvider : IContentProvider
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public EmbeddedContentProvider(IEnumerable<Assembly> assemblies)
        {
            _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
        }

        public bool CanProvideContent(string path)
        {
            return path.StartsWith(FileLoader.EmbeddedPrefix);
        }

        public string GetContent(string path)
        {
            if (!path.StartsWith(FileLoader.EmbeddedPrefix))
                throw new ArgumentException($"Path must start with {FileLoader.EmbeddedPrefix}", nameof(path));

            var resourcePath = path.Substring(FileLoader.EmbeddedPrefix.Length);

            foreach (var assembly in _assemblies)
            {
                try
                {
                    // embedded:Shaders/Common.glsl -> YourNamespace.Shaders.Common.glsl
                    var namespaceName = assembly.GetName().Name;
                    var resourceName = $"{namespaceName}.{resourcePath.Replace('/', '.').Replace('\\', '.')}";

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch { }
            }

            throw new FileNotFoundError($"Embedded resource not found: {resourcePath}");
        }

        public string ResolvePath(string basePath, string includePath)
        {
            if (includePath.StartsWith(FileLoader.EmbeddedPrefix))
                return includePath;

            if (basePath.StartsWith(FileLoader.EmbeddedPrefix))
            {
                var baseDir = Path.GetDirectoryName(basePath.Substring(FileLoader.EmbeddedPrefix.Length))?.Replace('\\', '/');
                return $"{FileLoader.EmbeddedPrefix}{baseDir}/{includePath}";
            }

            return includePath;
        }

        /// <summary>
        /// Условность: Путь с большой буквы, имя файла с большой буквы, расширения с маленькой.
        /// embedded:Resources/Geometry/Standart/Models/Cone.obj.meta ->
        /// EngineLib.Resources.Geometry.Standart.Models.Cone.obj.meta
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mask"></param>
        /// <param name="depthSearch"></param>
        /// <returns></returns>
        public List<string> SearchFilesByMask(string path, string mask, bool depthSearch)
        {
            var result = new List<string>();

            if (!path.StartsWith(FileLoader.EmbeddedPrefix))
                path = $"{FileLoader.EmbeddedPrefix}{path}";

            string resourcePathPrefix = path.Substring(FileLoader.EmbeddedPrefix.Length).Replace('\\', '/');
            if (!string.IsNullOrEmpty(resourcePathPrefix) && !resourcePathPrefix.EndsWith("/"))
                resourcePathPrefix += "/";

            string pattern = mask.Replace(".", "\\.").Replace("*", ".*").Replace("?", ".");
            var regex = new System.Text.RegularExpressions.Regex($"^{pattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var assembly in _assemblies)
            {
                var namespaceName = assembly.GetName().Name;
                var allResources = assembly.GetManifestResourceNames();
                string namespacePrefix = $"{namespaceName}.{resourcePathPrefix.Replace('/', '.')}";

                foreach (var resource in allResources)
                {
                    if (!resource.StartsWith(namespacePrefix))
                        continue;

                    string relativePath = resource.Substring(namespaceName.Length + 1);

                    var parts = relativePath.Split('.');

                    int fileNameIndex = -1;
                    for (int i = parts.Length - 1; i >= 0; i--)
                    {
                        if (parts[i].Length > 0 && char.IsUpper(parts[i][0]))
                        {
                            fileNameIndex = i;
                            break;
                        }
                    }

                    if (fileNameIndex >= 0)
                    {
                        string fileName = string.Join(".", parts.Skip(fileNameIndex));
                        if (regex.IsMatch(fileName))
                        {
                            string dirPath = string.Join("/", parts.Take(fileNameIndex));
                            string resourcePath = $"{dirPath}/{fileName}";

                            if (!depthSearch)
                            {
                                string searchDirNormalized = resourcePathPrefix.TrimEnd('/');
                                string fileDirNormalized = dirPath;

                                if (fileDirNormalized != searchDirNormalized)
                                    continue;
                            }

                            result.Add($"{FileLoader.EmbeddedPrefix}{resourcePath}");
                        }
                    }
                }
            }

            return result;
        }

        public byte[] GetBinaryContent(string path)
        {
            if (!path.StartsWith(FileLoader.EmbeddedPrefix))
                throw new ArgumentException($"Path must start with {FileLoader.EmbeddedPrefix}", nameof(path));

            var resourcePath = path.Substring(FileLoader.EmbeddedPrefix.Length);

            foreach (var assembly in _assemblies)
            {
                try
                {
                    var namespaceName = assembly.GetName().Name;
                    var resourceName = $"{namespaceName}.{resourcePath.Replace('/', '.').Replace('\\', '.')}";

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);
                                return memoryStream.ToArray();
                            }
                        }
                    }
                }
                catch { }
            }

            throw new FileNotFoundError($"Embedded binary resource not found: {resourcePath}");
        }

        public bool IsExist(string path)
        {
            if (!path.StartsWith(FileLoader.EmbeddedPrefix))
                throw new ArgumentException($"Path must start with {FileLoader.EmbeddedPrefix}", nameof(path));

            var resourcePath = path.Substring(FileLoader.EmbeddedPrefix.Length);

            foreach (var assembly in _assemblies)
            {
                try
                {
                    var namespaceName = assembly.GetName().Name;
                    var resourceName = $"{namespaceName}.{resourcePath.Replace('/', '.').Replace('\\', '.')}";

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            return false;
        }
    }

}
