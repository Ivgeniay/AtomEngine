using System.Text.RegularExpressions;

namespace OpenglLib
{
    public static class RSParser
    {
        private static Dictionary<string, string> masks = new Dictionary<string, string>()
        {
            { "InterfaceName", @"\[InterfaceName:[^\]]+\]" },
            { "RequiredComponent", @"\[RequiredComponent:[^\]]+\]" },
            { "ComponentName", @"\[ComponentName:[^\]]+\]" },
            { "SystemName", @"\[SystemName:[^\]]+\]" },
        };

        public static RSFileInfo ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundError($"RS file not found: {filePath}");

            string sourceCode = File.ReadAllText(filePath);
            return ParseContent(sourceCode, filePath);
        }

        public static RSFileInfo ParseContent(string sourceCode, string filePath)
        {
            var filename = Path.GetFileName(filePath);
            var folder = filePath.Substring(0, filePath.IndexOf(filename));
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath).Replace(".", "");

            var fileInfo = new RSFileInfo
            {
                SourcePath = filePath,
                SourceFolder = folder,
                InterfaceName = ExtractInterfaceName(sourceCode, fileNameWithoutExt),
                SystemName = ExtractSystemName(sourceCode, fileNameWithoutExt),
                ComponentName = ExtractComponentName(sourceCode, fileNameWithoutExt),
            };

            fileInfo.RequiredComponent = ExtractRequiredComponents(sourceCode);
            fileInfo.ProcessedCode = RemoveServiceMarkers(sourceCode);

            fileInfo.UniformBlocks = GlslParser.ParseUniformBlocks(fileInfo.ProcessedCode);
            fileInfo.Uniforms = GlslParser.ExtractUniforms(fileInfo.ProcessedCode);
            fileInfo.Structures = GlslParser.ParseGlslStructures(fileInfo.ProcessedCode);

            fileInfo.Methods = ExtractMethods(fileInfo.ProcessedCode);

            return fileInfo;
        }

        public static List<RSFileInfo> ProcessIncludes(string shaderSource, string sourcePath)
        {
            var processedPaths = new HashSet<string>();
            List<RSFileInfo> rsFiles;

            if (!string.IsNullOrEmpty(sourcePath))
            {
                shaderSource = IncludeProcessor.ProcessIncludes(shaderSource, sourcePath, processedPaths, out rsFiles);
            }
            else
            {
                rsFiles = new List<RSFileInfo>();
            }

            return rsFiles;
        }

        private static List<string> ExtractRequiredComponents(string sourceCode)
        {
            var components = new List<string>();
            var regex = new Regex(@"\[RequiredComponent\s*:\s*([^\]]+?)\s*\]", RegexOptions.Compiled);

            foreach (Match match in regex.Matches(sourceCode))
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    string componentName = match.Groups[1].Value.Trim();
                    components.Add(componentName);
                }
            }

            return components;
        }
        private static string ExtractInterfaceName(string sourceCode, string defaultName)
        {
            var match = Regex.Match(sourceCode, @"\[InterfaceName:([^\]]+)\]");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return "I" + defaultName + "Renderer";
        }

        private static string ExtractComponentName(string sourceCode, string defaultName)
        {
            var match = Regex.Match(sourceCode, @"\[ComponentName:([^\]]+)\]");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return defaultName + "Component";
        }

        private static string ExtractSystemName(string sourceCode, string defaultName)
        {
            var match = Regex.Match(sourceCode, @"\[SystemName:([^\]]+)\]");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return defaultName + "RendererSystem";
        }

        public static string RemoveServiceMarkers(string sourceCode)
        {
            foreach(var mask in masks.Values)
            {
                sourceCode = Regex.Replace(sourceCode, mask, "");
            }
            return sourceCode;
        }
        

        private static List<string> ExtractMethods(string sourceCode)
        {
            var methods = new List<string>();
            var methodRegex = new Regex(@"((?:void|float|int|vec\d|mat\d|bool)\s+\w+\s*\([^)]*\)\s*\{[^}]*\})", RegexOptions.Singleline);

            foreach (Match match in methodRegex.Matches(sourceCode))
            {
                methods.Add(match.Groups[1].Value);
            }

            return methods;
        }
    }
}
