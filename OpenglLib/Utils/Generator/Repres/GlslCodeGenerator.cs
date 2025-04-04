using AtomEngine;
using EngineLib;

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
                var shaderTypeManager = ServiceHub.Get<ShaderTypeManager>();
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
                        rsManager.UpdateRSFileTypesWithGeneratedNames(rsFile);

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

                if (shader.Vertex != null)
                {
                    foreach (var structure in shader.Vertex.Structures)
                    {
                        if (!processedStructures.Contains(structure.Name))
                        {
                            var typeInfo = shaderTypeManager.RegisterStructType(structure, sourcePath);
                            shaderTypeManager.EnsureTypeGenerated(typeInfo);
                            processedStructures.Add(structure.Name);
                        }
                    }
                }

                if (shader.Fragment != null)
                {
                    foreach (var structure in shader.Fragment.Structures)
                    {
                        if (!processedStructures.Contains(structure.Name))
                        {
                            var typeInfo = shaderTypeManager.RegisterStructType(structure, sourcePath);
                            shaderTypeManager.EnsureTypeGenerated(typeInfo);
                            processedStructures.Add(structure.Name);
                        }
                    }
                }

                if (shader.Vertex != null)
                {
                    foreach (var ubo in shader.Vertex.UniformsBlocks)
                    {
                        if (!processedUBOs.Contains(ubo.Name))
                        {
                            var typeInfo = shaderTypeManager.RegisterUniformBlockType(ubo, sourcePath);
                            shaderTypeManager.EnsureTypeGenerated(typeInfo);
                            processedUBOs.Add(ubo.Name);
                        }
                    }
                }

                if (shader.Fragment != null)
                {
                    foreach (var ubo in shader.Fragment.UniformsBlocks)
                    {
                        if (!processedUBOs.Contains(ubo.Name))
                        {
                            var typeInfo = shaderTypeManager.RegisterUniformBlockType(ubo, sourcePath);
                            shaderTypeManager.EnsureTypeGenerated(typeInfo);
                            processedUBOs.Add(ubo.Name);
                        }
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

                List<UniformBlockModel> allUBOs = new List<UniformBlockModel>();
                if (shader.Vertex != null)
                {
                    allUBOs.AddRange(shader.Vertex.UniformsBlocks);
                }

                if (shader.Fragment != null)
                {
                    foreach (var ubo in shader.Fragment.UniformsBlocks)
                    {
                        if (!allUBOs.Any(b => b.Name == ubo.Name))
                        {
                            allUBOs.Add(ubo);
                        }
                    }
                }

                foreach (var rsFile in shader.RSFiles)
                {
                    foreach (var block in rsFile.UniformBlocks)
                    {
                        if (!allUBOs.Any(b => b.Name == block.Name))
                        {
                            allUBOs.Add(block);
                        }
                    }
                }

                shaderTypeManager.UpdateTypeReferences(uniforms, allUBOs, shader.RSFiles);
                //shader.RSFiles.ForEach(s => { rsManager.UpdateRSFileTypesWithGeneratedNames(s); });

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

    }
}
