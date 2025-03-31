using System.Text;
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

            fileInfo.Constants = GlslParser.ExtractGlslConstants(fileInfo.ProcessedCode);
            fileInfo.UniformBlocks = GlslParser.ExtractUniformBlocks(fileInfo.ProcessedCode);
            fileInfo.Uniforms = GlslParser.ExtractUniforms(fileInfo.ProcessedCode);
            fileInfo.Structures = GlslParser.ExtractGlslStructures(fileInfo.ProcessedCode);
            fileInfo.Methods = GlslParser.ExtractMethods(fileInfo.ProcessedCode);

            fileInfo.StructureInstances = GlslParser.ExtractStructInstances(fileInfo.ProcessedCode, fileInfo.Structures);

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


        public static List<GlslConstantModel> GetConstFromFileInfos(List<RSFileInfo> rsInfos)
        {
            var consts = new List<GlslConstantModel>();
            foreach (RSFileInfo rsInfo in rsInfos)
            {
                consts.AddRange(rsInfo.Constants);
            }
            return consts;
        }
        public static List<UniformBlockModel> GetUniformsBlocksFromRsFileInfos(List<RSFileInfo> rsInfos)
        {
            var uniformsBlocks = new List<UniformBlockModel>();
            foreach (RSFileInfo rsInfo in rsInfos)
            {
                uniformsBlocks.AddRange(rsInfo.UniformBlocks);
            }
            return uniformsBlocks;
        }
        public static List<UniformModel> GetUniformsFromRsFileInfos(List<RSFileInfo> rsInfos)
        {
            var uniforms = new List<UniformModel>();
            foreach (RSFileInfo rsInfo in rsInfos)
            {
                uniforms.AddRange(rsInfo.Uniforms);
            }
            return uniforms;
        }
        public static List<GlslMethodInfo> GetMethodsFromRsFileInfos(List<RSFileInfo> rsInfos)
        {
            var methods = new List<GlslMethodInfo>();
            foreach (RSFileInfo rsInfo in rsInfos)
            {
                methods.AddRange(rsInfo.Methods);
            }
            return methods;
        }
        public static List<GlslStructModel> GetStructuresFromFileInfos(List<RSFileInfo> rsInfos)
        {
            var structs = new List<GlslStructModel>();
            foreach (RSFileInfo rsInfo in rsInfos)
            {
                structs.AddRange(rsInfo.Structures);
            }
            return structs;
        }
        public static List<GlslStructInstance> GetStructuresInstanceFromFileInfos(List<RSFileInfo> rsInfos)
        {
            var structs = new List<GlslStructInstance>();
            foreach (RSFileInfo rsInfo in rsInfos)
            {
                structs.AddRange(rsInfo.StructureInstances);
            }
            return structs;
        }
    }
}