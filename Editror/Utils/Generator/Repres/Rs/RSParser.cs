using System.Text.RegularExpressions;
using System.Collections.Generic;
using OpenglLib;
using System.IO;
using System;

namespace Editor
{
    public static class RSParser
    {
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
            var fileInfo = new RSFileInfo
            {
                SourcePath = filePath,
                SourceFolder = folder,
                InterfaceName = ExtractInterfaceName(sourceCode, Path.GetFileNameWithoutExtension(filePath))
            };

            fileInfo.ProcessedCode = RemoveServiceMarkers(sourceCode);

            fileInfo.UniformBlocks = GlslParser.ParseUniformBlocks(fileInfo.ProcessedCode);
            fileInfo.Uniforms = GlslParser.ExtractUniforms(fileInfo.ProcessedCode);
            fileInfo.Structures = GlslParser.ParseGlslStructures(fileInfo.ProcessedCode);

            fileInfo.Methods = ExtractMethods(fileInfo.ProcessedCode);

            return fileInfo;
        }

        public static List<RSFileInfo> ProcessIncludes(string shaderSource, string sourcePath)
        {
            var rsFiles = new List<RSFileInfo>();
            var processedPaths = new HashSet<string>();

            string ProcessIncludesRecursively(string source, string path)
            {
                if (processedPaths.Contains(path))
                    throw new CircularDependencyError($"Циклическая зависимость обнаружена: {path}");

                processedPaths.Add(path);

                var sourceDir = Path.GetDirectoryName(path);
                var includeRegex = new Regex(@"#include\s+""([^""]+)""");

                return includeRegex.Replace(source, match => {
                    var includePath = match.Groups[1].Value;
                    var fullPath = Path.GetFullPath(Path.Combine(sourceDir, includePath));

                    try
                    {
                        if (!File.Exists(fullPath))
                            throw new FileNotFoundException($"Включаемый файл не найден: {includePath}");

                        var includeContent = File.ReadAllText(fullPath);

                        if (Path.GetExtension(fullPath).ToLower() == ".rs")
                        {
                            var rsInfo = ParseContent(includeContent, fullPath);
                            rsFiles.Add(rsInfo);
                            return rsInfo.ProcessedCode;
                        }
                        return ProcessIncludesRecursively(includeContent, fullPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Ошибка при обработке включения '{includePath}': {ex.Message}", ex);
                    }
                });
            }

            if (!string.IsNullOrEmpty(sourcePath))
            {
                shaderSource = ProcessIncludesRecursively(shaderSource, sourcePath);
            }

            return rsFiles;
        }

        private static string ExtractInterfaceName(string sourceCode, string defaultName)
        {
            var match = Regex.Match(sourceCode, @"\[InterfaceName:([^\]]+)\]");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return "I" + defaultName + "Renderer";
        }

        public static string RemoveServiceMarkers(string sourceCode)
        {
            return Regex.Replace(sourceCode, @"\[InterfaceName:[^\]]+\]", "");
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
