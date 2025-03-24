using System.Collections.Generic;
using AtomEngine;
using System.IO;
using EngineLib;
using OpenglLib;
using System;
using System.Linq;

namespace Editor.Utils.Generator
{
    internal static class GlslCodeGenerator
    {
        public const string LABLE = "Rep.g";

        public static string GenerateCode(string sourcePath, string outputDirectory, bool generateStructs = true)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundError($"Shader file not found: {sourcePath}");
            }
            FileEvent fileEvent = new FileEvent();
            fileEvent.FileExtension = Path.GetExtension(sourcePath);
            fileEvent.FileFullPath = sourcePath;
            var assetPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
            fileEvent.FilePath = sourcePath.Substring(sourcePath.IndexOf(assetPath));
            fileEvent.FileName = Path.GetFileNameWithoutExtension(sourcePath);

            var result = GlslCompiler.TryToCompile(fileEvent);
            if (result.Success) DebLogger.Info(result);
            else
            {
                DebLogger.Error(result);
                return null;
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            try
            {
                string shaderSource = File.ReadAllText(sourcePath);

                string sourceGuid = ServiceHub.Get<EditorMetadataManager>().GetMetadata(sourcePath)?.Guid;
                string representationName = Path.GetFileNameWithoutExtension(sourcePath);
                List<RSFileInfo> rsFiles = RSParser.ProcessIncludes(shaderSource, sourcePath);

                var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(shaderSource, sourcePath);
                vertexSource = RSParser.RemoveServiceMarkers(vertexSource);
                fragmentSource = RSParser.RemoveServiceMarkers(fragmentSource);
                GlslParser.ValidateMainFunctions(vertexSource, fragmentSource);
                var combinedSource = vertexSource + "\n" + fragmentSource;

                List<GlslStructure> structures = new List<GlslStructure>();
                if (generateStructs)
                {
                    structures = GlslParser.ParseGlslStructures(combinedSource);
                    if (structures.Count > 0)
                    {
                        GlslStructGenerator.GenerateStructs(
                        shaderSourceCode: combinedSource,
                        outputDirectory: outputDirectory,
                        sourceGuid: sourceGuid);
                    }
                }
                List<UniformBlockStructure> uniformBlocks = GlslParser.ParseUniformBlocks(combinedSource);
                uniformBlocks = FreeBlocks(rsFiles, uniformBlocks);

                foreach (var block in uniformBlocks)
                {
                    block.CSharpTypeName = $"{block.Name}_{representationName}";
                    ShaderCodeRepresentationGenerator.GenerateUniformBlockClass(
                        block: block,
                        outputDirectory: outputDirectory,
                        sourceGuid: sourceGuid,
                        structures: structures);
                }
                foreach(var rs in rsFiles)
                {
                    foreach(var block in rs.UniformBlocks)
                    {
                        block.CSharpTypeName = $"{block.Name}_{block.UniformBlockType}";
                        ShaderCodeRepresentationGenerator.GenerateUniformBlockClass(
                            block: block,
                            outputDirectory: rs.SourceFolder,
                            sourceGuid: sourceGuid,
                            structures: rs.Structures);
                    }
                }
                foreach (var rs in rsFiles)
                {
                    var sourceCode = InterfaceGenerator.GenerateInterface(rs);
                    var path = Path.Combine(rs.SourceFolder, rs.InterfaceName + ".cs");
                    File.WriteAllText(path, sourceCode);
                }
                uniformBlocks = UnionBlocks(rsFiles, uniformBlocks);

                string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                    representationName: representationName,
                    outputDirectory: outputDirectory,
                    fragmentSource: fragmentSource,
                    uniformBlocks: uniformBlocks,
                    vertexSource: vertexSource,
                    sourceGuid: sourceGuid,
                    sourcePath: sourcePath,
                    rsFiles: rsFiles);

                return resultRepresentationName;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"{ex.Message}");
                return null;
            }
        }

        private static List<UniformBlockStructure> UnionBlocks(List<RSFileInfo> rsFiles, List<UniformBlockStructure> uniformBlocks)
        {
            var resultList = new List<UniformBlockStructure>();
            foreach (var rs in rsFiles)
            {
                foreach(var uniformBlock in rs.UniformBlocks)
                {
                    resultList.Add(uniformBlock);
                }
            }
            
            foreach(var block in uniformBlocks)
            {
                resultList.Add(block);
            }

            return resultList;
        }

        private static List<UniformBlockStructure> FreeBlocks(List<RSFileInfo> rsFiles, List<UniformBlockStructure> uniformBlocks)
        {
            return uniformBlocks.Where(e =>
            {
                foreach (var rs in rsFiles)
                {
                    if (!rs.UniformBlocks.Contains(e)) return true;
                }
                return false;
            }).ToList();
        }

        public static List<string> GenerateCodeFromDirectory(string directoryPath, string outputDirectory,
            string searchPattern = "*.glsl", bool generateStructs = true)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var generatedMaterials = new List<string>();
            var shaderFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);

            foreach (var shaderFile in shaderFiles)
            {
                try
                {
                    var shaderSource = File.ReadAllText(shaderFile);
                    if (GlslParser.IsCompleteShaderFile(shaderSource))
                    {
                        var fileName = GenerateCode(shaderFile, outputDirectory, generateStructs);
                        generatedMaterials.Add(fileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {shaderFile}: {ex.Message}");
                }
            }

            return generatedMaterials;
        }

    }
}
