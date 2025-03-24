using System.Collections.Generic;
using AtomEngine;
using System.IO;
using EngineLib;
using OpenglLib;
using System;

namespace Editor.Utils.Generator
{
    internal static class GlslCodeGenerator
    {
        public const string LABLE = "Rep.g";

        public static string GenerateCode(string glslFilePath, string outputDirectory, bool generateStructs = true)
        {

            if (!File.Exists(glslFilePath))
            {
                throw new FileNotFoundError($"Shader file not found: {glslFilePath}");
            }
            FileEvent fileEvent = new FileEvent();
            fileEvent.FileExtension = Path.GetExtension(glslFilePath);
            fileEvent.FileFullPath = glslFilePath;
            var assetPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
            fileEvent.FilePath = glslFilePath.Substring(glslFilePath.IndexOf(assetPath));
            fileEvent.FileName = Path.GetFileNameWithoutExtension(glslFilePath);

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
                string shaderSource = File.ReadAllText(glslFilePath);

                if (!GlslParser.IsCompleteShaderFile(shaderSource))
                {
                    throw new Exception($"The file {glslFilePath} is not a complete shader file (must contain #vertex and #fragment sections).");
                }

                string sourceGuid = ServiceHub.Get<EditorMetadataManager>().GetMetadata(glslFilePath)?.Guid;

                var representationName = Path.GetFileNameWithoutExtension(glslFilePath);
                var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(shaderSource, glslFilePath);
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
                foreach (var block in uniformBlocks)
                {
                    var blockClassName = $"{block.Name}_{representationName}"; 
                    ShaderCodeRepresentationGenerator.GenerateUniformBlockClass(
                        block: block,
                        className: blockClassName,
                        outputDirectory: outputDirectory,
                        representationName: representationName,
                        sourceGuid: sourceGuid,
                        structures: structures);
                }

                string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                    representationName: representationName,
                    shaderSource: shaderSource,
                    outputDirectory: outputDirectory,
                    sourceGuid: sourceGuid,
                    sourcePath: glslFilePath);

                return resultRepresentationName;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"{ex.Message}");
                return null;
            }
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
