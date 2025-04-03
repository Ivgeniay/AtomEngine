using AtomEngine;
using EngineLib;
using System.Text;

namespace OpenglLib
{
    public static class GlslCodeGenerator
    {
        public async static Task<string> GenerateCode(string sourcePath, string outputDirectory, string sourceGuid = null)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            try
            {
                string representationName = Path.GetFileNameWithoutExtension(sourcePath);
                var rsManager = ServiceHub.Get<RSManager>();
                var shader = GlslExtractor.ExtractShaderModel(sourcePath);

                HashSet<string> processedStructures = new HashSet<string>();
                HashSet<string> processedUBOs = new HashSet<string>();

                if (shader.RSFiles.Count > 0)
                {
                    foreach (var rsFile in shader.RSFiles)
                    {
                        string rsContent = FileLoader.LoadFile(rsFile.SourcePath);
                        var registration = rsManager.RegisterRSFile(rsFile.SourcePath, rsContent);
                        await rsManager.EnsureTypesGenerated(rsFile.SourcePath);

                        foreach (var structInfo in registration.Structures.Values)
                        {
                            processedStructures.Add(structInfo.OriginalName);
                        }

                        foreach (var uboInfo in registration.UniformBlocks.Values)
                        {
                            processedUBOs.Add(uboInfo.OriginalName);
                        }
                    }
                }

                List<GlslStructModel> uniqueStructures = new List<GlslStructModel>();
                Dictionary<string, GlslStructModel> structuresByName = new Dictionary<string, GlslStructModel>();

                if (shader.Vertex != null)
                {
                    foreach (var structure in shader.Vertex.Structures)
                    {
                        if (!processedStructures.Contains(structure.Name) && !structuresByName.ContainsKey(structure.Name))
                        {
                            structuresByName[structure.Name] = structure;
                            uniqueStructures.Add(structure);
                        }
                    }
                }

                if (shader.Fragment != null)
                {
                    foreach (var structure in shader.Fragment.Structures)
                    {
                        if (!processedStructures.Contains(structure.Name) && !structuresByName.ContainsKey(structure.Name))
                        {
                            structuresByName[structure.Name] = structure;
                            uniqueStructures.Add(structure);
                        }
                    }
                }

                if (uniqueStructures.Count > 0)
                {
                    foreach (var structure in uniqueStructures)
                    {
                        string structCode = GlslStructGenerator.GenerateModelClass(structure, sourceGuid);
                        string structPath = Path.Combine(outputDirectory, $"GlslStruct.{structure.Name}.g.cs");
                        File.WriteAllText(structPath, structCode, Encoding.UTF8);
                    }
                }

                List<UniformBlockModel> uniqueUBOs = new List<UniformBlockModel>();
                Dictionary<string, UniformBlockModel> ubosByName = new Dictionary<string, UniformBlockModel>();

                if (shader.Vertex != null)
                {
                    foreach (var ubo in shader.Vertex.UniformsBlocks)
                    {
                        if (!processedUBOs.Contains(ubo.Name) && !ubosByName.ContainsKey(ubo.Name))
                        {
                            ubo.CSharpTypeName = $"{ubo.Name}_{representationName}";
                            ubosByName[ubo.Name] = ubo;
                            uniqueUBOs.Add(ubo);
                        }
                    }
                }

                if (shader.Fragment != null)
                {
                    foreach (var ubo in shader.Fragment.UniformsBlocks)
                    {
                        if (!processedUBOs.Contains(ubo.Name) && !ubosByName.ContainsKey(ubo.Name))
                        {
                            ubo.CSharpTypeName = $"{ubo.Name}_{representationName}";
                            ubosByName[ubo.Name] = ubo;
                            uniqueUBOs.Add(ubo);
                        }
                    }
                }

                foreach (var ubo in uniqueUBOs)
                {
                    GlslsUBOGenerator.GenerateUniformBlockClass(
                        ubo,
                        outputDirectory,
                        sourceGuid,
                        uniqueStructures);
                }

                List<UniformBlockModel> allUBOs = new List<UniformBlockModel>(uniqueUBOs);

                foreach (var rs in shader.RSFiles)
                {
                    foreach (var block in rs.UniformBlocks)
                    {
                        RSUBOTypeInfo uboTypeInfo = rsManager.GetUBOTypeInfo(block.Name, rs.SourcePath);
                        if (uboTypeInfo != null)
                        {
                            block.CSharpTypeName = uboTypeInfo.GeneratedTypeName;
                        }
                        else
                        {
                            block.CSharpTypeName = $"{block.Name}_{block.UniformBlockType}";
                        }

                        allUBOs.Add(block);
                    }
                }

                List<UniformModel> uniforms = new List<UniformModel>();
                HashSet<string> uniformNames = new HashSet<string>();

                if (shader.Vertex != null)
                {
                    foreach (var uniform in shader.Vertex.Uniforms)
                    {
                        if (!uniformNames.Contains(uniform.Name))
                        {
                            uniformNames.Add(uniform.Name);
                            uniforms.Add(uniform);
                        }
                    }
                }

                if (shader.Fragment != null)
                {
                    foreach (var uniform in shader.Fragment.Uniforms)
                    {
                        if (!uniformNames.Contains(uniform.Name))
                        {
                            uniformNames.Add(uniform.Name);
                            uniforms.Add(uniform);
                        }
                    }
                }

                string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                    representationName: representationName,
                    outputDirectory: outputDirectory,
                    fragmentSource: shader.Fragment?.FullText ?? "",
                    vertexSource: shader.Vertex?.FullText ?? "",
                    uniforms: uniforms,
                    uniformBlocks: allUBOs,
                    sourceGuid: sourceGuid,
                    sourcePath: sourcePath,
                    rsFiles: shader.RSFiles);

                return resultRepresentationName;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"{ex.Message}");
                return null;
            }
        }
        private static List<UniformBlockModel> UnionBlocks(List<RSFileInfo> rsFiles, List<UniformBlockModel> uniformBlocks)
        {
            var resultList = new List<UniformBlockModel>();
            foreach (var rs in rsFiles)
            {
                foreach (var uniformBlock in rs.UniformBlocks)
                {
                    resultList.Add(uniformBlock);
                }
            }

            foreach (var block in uniformBlocks)
            {
                resultList.Add(block);
            }

            return resultList;
        }

        //public async static Task<string> GenerateCode(string sourcePath, string outputDirectory, string sourceGuid = null)
        //{
        //    if (!Directory.Exists(outputDirectory))
        //    {
        //        Directory.CreateDirectory(outputDirectory);
        //    }

        //    try
        //    {
        //        string representationName = Path.GetFileNameWithoutExtension(sourcePath);

        //        var shader = GlslExtractor.ExtractShaderModel(sourcePath);

        //        List<GlslConstantModel> constants = GlslParser.ExtractGlslConstants(shader.FullText);
        //        List<UniformModel> uniforms = GlslParser.ExtractUniforms(shader.FullText);
        //        List<GlslStructModel> structures = structures = GlslParser.ExtractGlslStructures(shader.FullText);

        //        if (structures.Count > 0)
        //        {
        //            GlslStructGenerator.GenerateStructs(
        //                shaderSourceCode: shader.FullText,
        //                outputDirectory: outputDirectory,
        //                sourceGuid: sourceGuid);
        //        }

        //        List<UniformBlockModel> uniformBlocks = GlslParser.ExtractUniformBlocks(shader.FullText);
        //        uniformBlocks = SeparateBlocks(shader.RSFiles, uniformBlocks);

        //        foreach (var block in uniformBlocks)
        //        {
        //            block.CSharpTypeName = $"{block.Name}_{representationName}";
        //            GlslsUBOGenerator.GenerateUniformBlockClass(
        //                block: block,
        //                outputDirectory: outputDirectory,
        //                sourceGuid: sourceGuid,
        //                structures: structures);
        //        }

        //        foreach (var rs in shader.RSFiles)
        //        {
        //            foreach (var block in rs.UniformBlocks)
        //            {
        //                string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
        //                block.CSharpTypeName = $"{block.Name}_{block.UniformBlockType}";
        //                GlslsUBOGenerator.GenerateUniformBlockClass(
        //                    block: block,
        //                    outputDirectory: path,
        //                    sourceGuid: sourceGuid,
        //                    structures: rs.Structures);
        //            }
        //        }

        //        foreach (var rs in shader.RSFiles)
        //        {
        //            var sourceCode = InterfaceGenerator.GenerateInterface(rs);
        //            string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
        //            path = Path.Combine(path, rs.InterfaceName + ".cs");
        //            File.WriteAllText(path, sourceCode);
        //        }

        //        Dictionary<RSFileInfo, ComponentGeneratorInfo> compListsMap = new();
        //        foreach (var rs in shader.RSFiles)
        //        {
        //            ComponentGeneratorInfo componentInfo;
        //            var sourceCode = ComponentGenerator.GenerateComponentTemplate(rs, out componentInfo);
        //            string componentFileName = rs.ComponentName;
        //            string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
        //            path = Path.Combine(path, componentFileName + ".cs");
        //            compListsMap[rs] = componentInfo;
        //            File.WriteAllText(path, sourceCode);
        //        }

        //        foreach (var rs in shader.RSFiles)
        //        {
        //            ComponentGeneratorInfo component = null;
        //            if (compListsMap.TryGetValue(rs, out component)) { }
        //            var sourceCode = RenderSystemGenerator.GenerateRenderSystemTemplate(rs, component);
        //            string systemFileName = rs.SystemName;
        //            string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
        //            path = Path.Combine(path, systemFileName + ".cs");
        //            File.WriteAllText(path, sourceCode);
        //        }


        //        uniformBlocks = UnionBlocks(shader.RSFiles, uniformBlocks);

        //        string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
        //            representationName: representationName,
        //            outputDirectory: outputDirectory,
        //            fragmentSource: shader.Fragment.FullText,
        //            vertexSource: shader.Vertex.FullText,
        //            uniforms: uniforms,
        //            uniformBlocks: uniformBlocks,
        //            sourceGuid: sourceGuid,
        //            sourcePath: sourcePath,
        //            rsFiles: shader.RSFiles);

        //        return resultRepresentationName;
        //    }
        //    catch (Exception ex)
        //    {
        //        DebLogger.Error($"{ex.Message}");
        //        return null;
        //    }
        //}

        //private static List<UniformBlockModel> UnionBlocks(List<RSFileInfo> rsFiles, List<UniformBlockModel> uniformBlocks)
        //{
        //    var resultList = new List<UniformBlockModel>();
        //    foreach (var rs in rsFiles)
        //    {
        //        foreach(var uniformBlock in rs.UniformBlocks)
        //        {
        //            resultList.Add(uniformBlock);
        //        }
        //    }

        //    foreach(var block in uniformBlocks)
        //    {
        //        resultList.Add(block);
        //    }

        //    return resultList;
        //}
        //private static List<UniformBlockModel> SeparateBlocks(List<RSFileInfo> rsFiles, List<UniformBlockModel> uniformBlocks)
        //{
        //    return uniformBlocks.Where(e =>
        //    {
        //        foreach (var rs in rsFiles)
        //        {
        //            foreach (var ubo in rs.UniformBlocks)
        //            {
        //                if (ubo.Name == e.Name) return false;
        //            }
        //        }

        //        return true;
        //    }).ToList();
        //}


    }
}
