using System.Text.RegularExpressions;
using System.Reflection;
using EngineLib;

namespace OpenglLib
{
    public static class IncludeProcessor
    {
        private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""");

        public static void RegisterContentProvider(IContentProvider provider)
        {
            FileLoader.RegisterContentProvider(provider);
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
                string fullPath;
                string includeContent;

                try
                {
                    fullPath = FileLoader.ResolvePath(sourcePath, includePath);
                    includeContent = FileLoader.LoadFile(fullPath);
                }
                catch (Exception ex)
                {
                    throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
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


}
