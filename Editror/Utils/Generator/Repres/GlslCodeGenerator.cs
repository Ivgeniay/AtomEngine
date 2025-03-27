using System.Collections.Generic;
using AtomEngine;
using System.IO;
using EngineLib;
using OpenglLib;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Editor.Utils.Generator
{
    internal static class GlslCodeGenerator
    {
        public const string LABLE = "Rep.g";

        public async static Task<string> GenerateCode(string sourcePath, string outputDirectory, bool generateStructs = true)
        {
            var loadingManager = ServiceHub.Get<LoadingManager>();
            string finalResult = null;
            CsCompileWatcher watcher = ServiceHub.Get<CsCompileWatcher>();

            try
            {
                watcher.EnableWatching(false);
                await loadingManager.RunWithLoading(async (progress) =>
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
                    return;
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

                    shaderSource = GlslParser.ProcessIncludesRecursively(shaderSource, sourcePath);
                    shaderSource = RSParser.RemoveServiceMarkers(shaderSource);

                    var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(shaderSource);
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
                    uniformBlocks = SeparateBlocks(rsFiles, uniformBlocks);

                    foreach (var block in uniformBlocks)
                    {
                        block.CSharpTypeName = $"{block.Name}_{representationName}";
                        GlslsUBOGenerator.GenerateUniformBlockClass(
                            block: block,
                            outputDirectory: outputDirectory,
                            sourceGuid: sourceGuid,
                            structures: structures);
                    }

                    foreach (var rs in rsFiles)
                    {
                        foreach (var block in rs.UniformBlocks)
                        {
                            string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                            block.CSharpTypeName = $"{block.Name}_{block.UniformBlockType}";
                            GlslsUBOGenerator.GenerateUniformBlockClass(
                                block: block,
                                outputDirectory: path,
                                sourceGuid: sourceGuid,
                                structures: rs.Structures);
                        }
                    }

                    foreach (var rs in rsFiles)
                    {
                        var sourceCode = InterfaceGenerator.GenerateInterface(rs);
                        string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                        path = Path.Combine(path, rs.InterfaceName + ".cs");
                        File.WriteAllText(path, sourceCode);
                    }

                    Dictionary<RSFileInfo, ComponentGeneratorInfo> compListsMap = new();
                    foreach (var rs in rsFiles)
                    {
                        ComponentGeneratorInfo componentInfo;
                        var sourceCode = ComponentGenerator.GenerateComponentTemplate(rs, out componentInfo);
                        string componentFileName = ComponentGenerator.GetComponentNameFromInterface(rs.InterfaceName);
                        string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                        path = Path.Combine(path, componentFileName + ".cs");
                        compListsMap[rs] = componentInfo;
                        File.WriteAllText(path, sourceCode);
                    }

                    foreach (var rs in rsFiles)
                    {
                        ComponentGeneratorInfo component = null;
                        if (compListsMap.TryGetValue(rs, out component)) { }
                        var sourceCode = RenderSystemGenerator.GenerateRenderSystemTemplate(rs, component);
                        string systemFileName = RenderSystemGenerator.GetSystemNameFromInterface(rs.InterfaceName);
                        string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                        path = Path.Combine(path, systemFileName + ".cs");
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

                    finalResult = resultRepresentationName;
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"{ex.Message}");
                    return;
                }
            });
                await Task.Delay(1500);
                ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                await ServiceHub.Get<ScriptSyncSystem>().RebuildProject(pConf.BuildType);
                await Task.Delay(1000);
            }
            finally
            {
                watcher.EnableWatching(true);
            }
            return finalResult;
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
        private static List<UniformBlockStructure> SeparateBlocks(List<RSFileInfo> rsFiles, List<UniformBlockStructure> uniformBlocks)
        {
            return uniformBlocks.Where(e =>
            {
                foreach (var rs in rsFiles)
                {
                    foreach (var ubo in rs.UniformBlocks)
                    {
                        if (ubo.Name == e.Name) return false;
                    }
                }

                return true;
            }).ToList();

            //return uniformBlocks.Where(e =>
            //{
            //    foreach (var rs in rsFiles)
            //    {
            //        if (!rs.UniformBlocks.Contains(e)) return true;
            //    }
            //    return false;
            //}).ToList();
        }
    }
}
