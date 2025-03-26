using System.Text.RegularExpressions;
using System.Reflection;

namespace OpenglLib
{
    public static class IncludeProcessor
    {
        private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""");
        private static readonly List<IContentProvider> ContentProviders = new List<IContentProvider>();
        public const string EmbeddedPrefix = "embedded:";

        //static IncludeProcessor()
        //{
        //    RegisterContentProvider(new FileSystemContentProvider());
        //    RegisterContentProvider(new EmbeddedContentProvider(new[] { Assembly.GetExecutingAssembly() }));
        //}

        public static void RegisterContentProvider(IContentProvider provider)
        {
            ContentProviders.Add(provider);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, bool collectRsFiles, out List<RSFileInfo> rsFiles)
        {
            rsFiles = collectRsFiles ? new List<RSFileInfo>() : null;
            return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths)
        {
            return ProcessIncludesInternal(source, sourcePath, processedPaths, null);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();
            return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
        }

        private static string ProcessIncludesInternal(string source, string sourcePath, HashSet<string> processedPaths, List<RSFileInfo> rsFiles)
        {
            processedPaths ??= new HashSet<string>();

            if (processedPaths.Contains(sourcePath))
                throw new CircularDependencyError($"Cyclic dependency detected: {sourcePath}");

            processedPaths.Add(sourcePath);

            return IncludeRegex.Replace(source, match => {
                var includePath = match.Groups[1].Value;

                string fullPath = null;
                string includeContent = null;
                Exception lastException = null;

                if (!includePath.StartsWith(EmbeddedPrefix))
                {
                    var fileProvider = ContentProviders.FirstOrDefault(p => p is FileSystemContentProvider);
                    if (fileProvider != null)
                    {
                        try
                        {
                            fullPath = fileProvider.ResolvePath(sourcePath, includePath);
                            includeContent = fileProvider.GetContent(fullPath);
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                        }
                    }
                }

                if (includeContent == null)
                {
                    var embeddedProvider = ContentProviders.FirstOrDefault(p => p is EmbeddedContentProvider);
                    if (embeddedProvider != null)
                    {
                        try
                        {
                            var embeddedPath = includePath.StartsWith(EmbeddedPrefix)
                                ? includePath
                                : $"{EmbeddedPrefix}{includePath}";

                            fullPath = embeddedProvider.ResolvePath(sourcePath, embeddedPath);
                            includeContent = embeddedProvider.GetContent(fullPath);
                        }
                        catch (Exception ex)
                        {
                            if (lastException == null)
                                lastException = ex;
                        }
                    }
                }

                if (includeContent == null)
                {
                    if (lastException != null)
                        throw new ShaderError($"Error processing include: '{includePath}': {lastException.Message}");
                    else
                        throw new FileNotFoundError($"Include file not found: {includePath}");
                }

                try
                {
                    if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase) && rsFiles != null)
                    {
                        var rsInfo = RSParser.ParseContent(includeContent, fullPath);
                        rsFiles.Add(rsInfo);
                        return rsInfo.ProcessedCode;
                    }
                    return ProcessIncludesInternal(includeContent, fullPath, new HashSet<string>(processedPaths), rsFiles);
                }
                catch (Exception ex)
                {
                    throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
                }
            });
        }
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
            return !path.StartsWith(IncludeProcessor.EmbeddedPrefix);
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
            return path.StartsWith(IncludeProcessor.EmbeddedPrefix);
        }

        public string GetContent(string path)
        {
            if (!path.StartsWith(IncludeProcessor.EmbeddedPrefix))
                throw new ArgumentException($"Path must start with {IncludeProcessor.EmbeddedPrefix}", nameof(path));

            var resourcePath = path.Substring(IncludeProcessor.EmbeddedPrefix.Length);

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
            if (includePath.StartsWith(IncludeProcessor.EmbeddedPrefix))
                return includePath;

            if (basePath.StartsWith(IncludeProcessor.EmbeddedPrefix))
            {
                var baseDir = Path.GetDirectoryName(basePath.Substring(IncludeProcessor.EmbeddedPrefix.Length))?.Replace('\\', '/');
                return $"{IncludeProcessor.EmbeddedPrefix}{baseDir}/{includePath}";
            }

            return includePath;
        }
    }
}
