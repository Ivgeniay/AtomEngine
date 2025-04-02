using AtomEngine;

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

                var shader = GlslExtractor.ExtractShaderModel(sourcePath);

                List<GlslConstantModel> constants = GlslParser.ExtractGlslConstants(shader.FullText);
                List<UniformModel> uniforms = GlslParser.ExtractUniforms(shader.FullText);
                List<GlslStructModel> structures = structures = GlslParser.ExtractGlslStructures(shader.FullText);

                if (structures.Count > 0)
                {
                    GlslStructGenerator.GenerateStructs(
                        shaderSourceCode: shader.FullText,
                        outputDirectory: outputDirectory,
                        sourceGuid: sourceGuid);
                }

                List<UniformBlockModel> uniformBlocks = GlslParser.ExtractUniformBlocks(shader.FullText);
                uniformBlocks = SeparateBlocks(shader.RSFiles, uniformBlocks);

                foreach (var block in uniformBlocks)
                {
                    block.CSharpTypeName = $"{block.Name}_{representationName}";
                    GlslsUBOGenerator.GenerateUniformBlockClass(
                        block: block,
                        outputDirectory: outputDirectory,
                        sourceGuid: sourceGuid,
                        structures: structures);
                }

                foreach (var rs in shader.RSFiles)
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

                foreach (var rs in shader.RSFiles)
                {
                    var sourceCode = InterfaceGenerator.GenerateInterface(rs);
                    string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                    path = Path.Combine(path, rs.InterfaceName + ".cs");
                    File.WriteAllText(path, sourceCode);
                }

                Dictionary<RSFileInfo, ComponentGeneratorInfo> compListsMap = new();
                foreach (var rs in shader.RSFiles)
                {
                    ComponentGeneratorInfo componentInfo;
                    var sourceCode = ComponentGenerator.GenerateComponentTemplate(rs, out componentInfo);
                    string componentFileName = rs.ComponentName;
                    string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                    path = Path.Combine(path, componentFileName + ".cs");
                    compListsMap[rs] = componentInfo;
                    File.WriteAllText(path, sourceCode);
                }

                foreach (var rs in shader.RSFiles)
                {
                    ComponentGeneratorInfo component = null;
                    if (compListsMap.TryGetValue(rs, out component)) { }
                    var sourceCode = RenderSystemGenerator.GenerateRenderSystemTemplate(rs, component);
                    string systemFileName = rs.SystemName;
                    string path = rs.SourcePath.Contains(":") ? outputDirectory : rs.SourceFolder;
                    path = Path.Combine(path, systemFileName + ".cs");
                    File.WriteAllText(path, sourceCode);
                }


                uniformBlocks = UnionBlocks(shader.RSFiles, uniformBlocks);

                string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                    representationName: representationName,
                    outputDirectory: outputDirectory,
                    fragmentSource: shader.Fragment.FullText,
                    vertexSource: shader.Vertex.FullText,
                    uniforms: uniforms,
                    uniformBlocks: uniformBlocks,
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
