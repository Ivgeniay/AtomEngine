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
        public async static Task<string> GenerateCode(string sourcePath, string outputDirectory, bool generateStructs = true)
        {
            var loadingManager = ServiceHub.Get<LoadingManager>();
            string finalResult = null;
            CsCompileWatcher watcher = ServiceHub.Get<CsCompileWatcher>();
            CompilationGlslCodeResult result = null;

            var shader = GlslExtractor.ExtractShader(sourcePath);
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

                    result = GlslCompiler.TryToCompile(fileEvent);
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
                        shaderSource = GlslParser.RemoveAllAttributes(shaderSource);
                        shaderSource = GlslParser.ResolveConstantPlacement(shaderSource, RSParser.GetConstFromFileInfos(rsFiles));
                        shaderSource = GlslParser.ResolveStructurePlacement(shaderSource, RSParser.GetStructuresFromFileInfos(rsFiles));
                        shaderSource = GlslParser.ResolveUniformPlacement(shaderSource, RSParser.GetUniformsFromRsFileInfos(rsFiles));
                        shaderSource = GlslParser.ResolveUniformBlockPlacement(shaderSource, RSParser.GetUniformsBlocksFromRsFileInfos(rsFiles));
                        shaderSource = GlslParser.ResolveMethodPlacement(shaderSource, RSParser.GetMethodsFromRsFileInfos(rsFiles));

                        var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(shaderSource);
                        GlslParser.ValidateMainFunctions(vertexSource, fragmentSource);

                        var combinedSource = vertexSource + "\n" + fragmentSource;

                        List<GlslConstantModel> constants = GlslParser.ParseGlslConstants(combinedSource);
                        List<UniformModel> uniforms = GlslParser.ExtractUniforms(combinedSource);
                        List<GlslStructureModel> structures = structures = GlslParser.ParseGlslStructures(combinedSource);

                        if (structures.Count > 0)
                        {
                            GlslStructGenerator.GenerateStructs(
                                shaderSourceCode: combinedSource,
                                outputDirectory: outputDirectory,
                                sourceGuid: sourceGuid);
                        }

                        List<UniformBlockModel> uniformBlocks = GlslParser.ParseUniformBlocks(combinedSource);
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
                            string componentFileName = rs.ComponentName;
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
                            string systemFileName = rs.SystemName;
                            string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                            path = Path.Combine(path, systemFileName + ".cs");
                            File.WriteAllText(path, sourceCode);
                        }


                        uniformBlocks = UnionBlocks(rsFiles, uniformBlocks);

                        string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                            representationName: representationName,
                            outputDirectory: outputDirectory,
                            fragmentSource: fragmentSource,
                            vertexSource: vertexSource,
                            uniforms: uniforms,
                            uniformBlocks: uniformBlocks,
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
                if (result != null && result.Success)
                {
                    ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                    await ServiceHub.Get<ScriptSyncSystem>().RebuildProject(pConf.BuildType);
                    await Task.Delay(1000);
                }
            }
            finally
            {
                watcher.EnableWatching(true);
            }
            return finalResult;
        }

        private static List<UniformBlockModel> UnionBlocks(List<RSFileInfo> rsFiles, List<UniformBlockModel> uniformBlocks)
        {
            var resultList = new List<UniformBlockModel>();
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
        private static List<UniformBlockModel> SeparateBlocks(List<RSFileInfo> rsFiles, List<UniformBlockModel> uniformBlocks)
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
        }
    }
}
