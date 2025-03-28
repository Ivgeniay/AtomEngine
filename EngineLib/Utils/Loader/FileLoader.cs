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

            if (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch)
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null && fileProvider.CanProvideContent(path))
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
                (searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch))
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    try
                    {
                        var embeddedPath = path.StartsWith(EmbeddedPrefix)
                            ? path
                            : $"{EmbeddedPrefix}{path}";

                        if (embeddedProvider.CanProvideContent(embeddedPath))
                        {
                            content = embeddedProvider.GetContent(embeddedPath);
                        }
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

        public static string ResolvePath(string basePath, string includePath, FileSearchMode searchMode = FileSearchMode.BothSearch)
        {
            string resolvedPath = null;

            if (searchMode == FileSearchMode.FileSystemOnly || searchMode == FileSearchMode.BothSearch)
            {
                var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                if (fileProvider != null && !includePath.StartsWith(EmbeddedPrefix))
                {
                    try
                    {
                        resolvedPath = fileProvider.ResolvePath(basePath, includePath);

                        if (File.Exists(resolvedPath))
                            return resolvedPath;
                    }
                    catch { }
                }
            }

            if (searchMode == FileSearchMode.EmbeddedOnly || searchMode == FileSearchMode.BothSearch)
            {
                var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                if (embeddedProvider != null)
                {
                    var embeddedPath = includePath.StartsWith(EmbeddedPrefix)
                        ? includePath
                        : $"{EmbeddedPrefix}{includePath}";

                    try
                    {
                        resolvedPath = embeddedProvider.ResolvePath(basePath.StartsWith(EmbeddedPrefix) ? basePath : $"{EmbeddedPrefix}{basePath}", embeddedPath);
                        return resolvedPath;
                    }
                    catch { }
                }
            }

            if (resolvedPath == null)
            {
                throw new FileNotFoundError($"Could not resolve path for: {includePath} relative to {basePath}");
            }

            return resolvedPath;
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
    }

}
