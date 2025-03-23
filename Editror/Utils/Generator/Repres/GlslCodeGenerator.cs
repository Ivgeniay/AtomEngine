using System.Collections.Generic;
using System.IO;
using EngineLib;
using System;
using OpenglLib;
using System.Text.RegularExpressions;
using AtomEngine;
using System.Text;
using Silk.NET.OpenAL;

namespace Editor.Utils.Generator
{
    internal static class GlslCodeGenerator
    {
        public const string LABLE = "Rep.g";

        public static string GenerateCode(string glslFilePath, string outputDirectory, bool generateStructs = true)
        {
            if (!File.Exists(glslFilePath))
            {
                throw new FileNotFoundException($"Shader file not found: {glslFilePath}");
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

                List<string> structuresText = new List<string>();
                if (generateStructs)
                {
                    List<GlslStructure> structures = GlslParser.ParseGlslStructures(combinedSource);
                    if (structures.Count > 0)
                    {
                        structuresText.AddRange(
                            GlslStructGenerator.GenerateStructs(
                            shaderSourceCode: combinedSource,
                            outputDirectory: outputDirectory,
                            sourceGuid: sourceGuid)
                            );
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
                        structureTexts: structuresText);
                }

                string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                    representationName: representationName,
                    sourceText: shaderSource,
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
