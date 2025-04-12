using System.Collections.Concurrent;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public static class GlslCodeGenerator
    {
        public async static Task GenerateCode(string sourcePath, string outputDirectory, ShaderUniformCacheData uniformCacheData, string sourceGuid = null)
        {
            if (uniformCacheData == null)
            {
                DebLogger.Error("Impossible generate shader C# representation withou uniform data");
                return;
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            try
            {
                GlslTypeMapper.Clear();

                string representationName = Path.GetFileNameWithoutExtension(sourcePath);
                var rsManager = ServiceHub.Get<RSManager>();
                var shaderTypeManager = ServiceHub.Get<ShaderTypeManager>();
                var shader = GlslExtractor.ExtractShaderModel(sourcePath);

                ShaderStructureInstanceProcessor.SyncRSFilesWithShaderModel(shader);

                HashSet<string> processedStructures = new HashSet<string>();
                HashSet<string> processedUBOs = new HashSet<string>();

                if (shader.RSFiles.Count > 0)
                {
                    foreach (var rsFile in shader.RSFiles)
                    {
                        string rsContent = FileLoader.LoadFile(rsFile.SourcePath);
                        var registration = rsManager.RegisterRSFile(rsFile.SourcePath, rsContent);
                        rsManager.CacheRSInstance(rsFile);
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

                foreach (var rsFile in shader.RSFiles)
                {
                    await rsManager.EnsureTypesGenerated(rsFile.SourcePath);
                    await rsManager.GenerateComponentsFromRSFile(rsFile);
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

                string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                    representationName: representationName,
                    outputDirectory: outputDirectory,
                    fragmentSource: shader.Fragment?.FullText ?? "",
                    vertexSource: shader.Vertex?.FullText ?? "",
                    uniforms: uniforms,
                    uniformBlocks: allUBOs,
                    sourceGuid: sourceGuid,
                    sourcePath: sourcePath,
                    shaderModel: shader,
                    rsFiles: shader.RSFiles);

            }
            catch (Exception ex)
            {
                DebLogger.Error($"{ex.Message}");
                return;
            }
        }

    }

    public class ShaderUniformCacheData
    {
        public Dictionary<string, int> UniformLocations = new Dictionary<string, int>();
        public Dictionary<string, uint> AttributeLocations = new Dictionary<string, uint>();
        public Dictionary<string, UniformInfo> UniformInfo = new Dictionary<string, UniformInfo>();
        public List<UniformBlockData> UniformBlocks = new List<UniformBlockData>();
    }

    public static class ShaderStructureInstanceProcessor
    {
        private const string VERTEX_SUFFIX = "_vt";
        private const string FRAGMENT_SUFFIX = "_fg";

        public static void ProcessStructureInstances(GlslShaderModel shaderModel)
        {
            if (shaderModel == null) return;

            var instanceMap = CollectStructureInstances(shaderModel);

            ProcessDuplicateInstances(instanceMap);
        }

        public static void SyncRSFilesWithShaderModel(GlslShaderModel shaderModel)
        {
            if (shaderModel == null || shaderModel.RSFiles == null || shaderModel.RSFiles.Count == 0)
                return;

            foreach (var rsFile in shaderModel.RSFiles)
            {
                SyncRSFileInstances(rsFile, shaderModel);
            }
        }

        private static Dictionary<string, List<InstanceInfo>> CollectStructureInstances(GlslShaderModel shaderModel)
        {
            var instanceMap = new Dictionary<string, List<InstanceInfo>>();

            if (shaderModel.Vertex != null)
            {
                foreach (var instance in shaderModel.Vertex.StructureInstances)
                {
                    if (!instance.IsUniform)
                    {
                        AddInstanceToMap(instanceMap, instance, true);
                    }
                }
            }

            if (shaderModel.Fragment != null)
            {
                foreach (var instance in shaderModel.Fragment.StructureInstances)
                {
                    if (!instance.IsUniform)
                    {
                        AddInstanceToMap(instanceMap, instance, false);
                    }
                }
            }

            return instanceMap;
        }

        private static void AddInstanceToMap(Dictionary<string, List<InstanceInfo>> instanceMap,
                                            GlslStructInstance instance, bool isVertex)
        {
            if (!instanceMap.ContainsKey(instance.InstanceName))
            {
                instanceMap[instance.InstanceName] = new List<InstanceInfo>();
            }

            instanceMap[instance.InstanceName].Add(new InstanceInfo
            {
                Instance = instance,
                IsVertex = isVertex,
                StructureHash = CalculateStructureHash(instance.Structure)
            });

            instance.OriginalInstanceName = instance.InstanceName;
        }

        private static void ProcessDuplicateInstances(Dictionary<string, List<InstanceInfo>> instanceMap)
        {
            foreach (var entry in instanceMap)
            {
                string instanceName = entry.Key;
                var instances = entry.Value;

                if (instances.Count <= 1) continue;

                bool hasVertexInstance = instances.Any(i => i.IsVertex);
                bool hasFragmentInstance = instances.Any(i => !i.IsVertex);

                if (!hasVertexInstance || !hasFragmentInstance) continue;

                bool areStructuresIdentical = AreStructuresIdentical(instances);
                RenameInstancesIfNeeded(instances, areStructuresIdentical);
            }
        }

        private static bool AreStructuresIdentical(List<InstanceInfo> instances)
        {
            if (instances.Count <= 1) return true;

            string firstHash = instances.First().StructureHash;
            return instances.All(i => i.StructureHash == firstHash);
        }

        private static void RenameInstancesIfNeeded(List<InstanceInfo> instances, bool areStructuresIdentical)
        {
            foreach (var info in instances)
            {
                if (info.IsVertex)
                {
                    info.Instance.InstanceName += VERTEX_SUFFIX;
                }
                else
                {
                    info.Instance.InstanceName += FRAGMENT_SUFFIX;
                }
            }
        }

        private static void SyncRSFileInstances(RSFileInfo rsFile, GlslShaderModel shaderModel)
        {
            foreach (var rsInstance in rsFile.StructureInstances)
            {
                if (rsInstance.IsUniform)
                    continue;

                if (shaderModel.Vertex != null)
                {
                    SyncInstanceWithShader(rsInstance, shaderModel.Vertex.StructureInstances);
                }

                if (shaderModel.Fragment != null && rsInstance.OriginalInstanceName == rsInstance.InstanceName)
                {
                    SyncInstanceWithShader(rsInstance, shaderModel.Fragment.StructureInstances);
                }
            }
        }

        private static void SyncInstanceWithShader(GlslStructInstance rsInstance, List<GlslStructInstance> shaderInstances)
        {
            var shaderInstance = shaderInstances.FirstOrDefault(i => !i.IsUniform && i.OriginalInstanceName == rsInstance.InstanceName);

            if (shaderInstance != null)
            {
                rsInstance.OriginalInstanceName = rsInstance.InstanceName;
                rsInstance.InstanceName = shaderInstance.InstanceName;
            }
        }

        private static string CalculateStructureHash(GlslStructModel structure)
        {
            if (structure == null) return string.Empty;
            return string.Join("|", structure.Fields.Select(f => $"{f.Type}:{f.Name}:{f.ArraySize}"));
        }

        private class InstanceInfo
        {
            public GlslStructInstance Instance { get; set; }
            public bool IsVertex { get; set; }
            public string StructureHash { get; set; }
        }
    }

    public static class GlslTypeMapper
    {
        private static RSManager rsManagerInstance = null;
        private static RSManager rsManager
        {
            get
            {
                if(rsManagerInstance == null) rsManagerInstance = ServiceHub.Get<RSManager>();
                return rsManagerInstance;
            }
        }

        private static ShaderTypeManager shaderTypeManagerInstance = null;
        private static ShaderTypeManager shaderTypeManager
        {
            get
            {
                if (shaderTypeManagerInstance == null) shaderTypeManagerInstance = ServiceHub.Get<ShaderTypeManager>();
                return shaderTypeManagerInstance;
            }
        }


        private static readonly ConcurrentDictionary<string, (string TypeName, string Guid)> _typeCache =
            new ConcurrentDictionary<string, (string, string)>();

        public static string TryFindStructureGuid(string structureName, string sourcePath = null)
        {
            if (_typeCache.TryGetValue(structureName, out var cached))
                return cached.Guid;


            var rsStructInfo = rsManager.GetStructTypeInfo(structureName, sourcePath);
            if (rsStructInfo != null && !string.IsNullOrEmpty(rsStructInfo.StructureGuid))
            {
                _typeCache[structureName] = (rsStructInfo.GeneratedTypeName, rsStructInfo.StructureGuid);
                return rsStructInfo.StructureGuid;
            }

            var shaderTypeInfo = shaderTypeManager.GetShaderTypeInfo(structureName, sourcePath);
            if (shaderTypeInfo != null && !string.IsNullOrEmpty(shaderTypeInfo.StructureGuid))
            {
                _typeCache[structureName] = (shaderTypeInfo.GeneratedTypeName, shaderTypeInfo.StructureGuid);
                return shaderTypeInfo.StructureGuid;
            }

            string newGuid = Guid.NewGuid().ToString();
            return newGuid;
        }

        public static string GetTypeNameByGuid(string structureGuid)
        {
            foreach (var entry in _typeCache.Values)
            {
                if (entry.Guid == structureGuid)
                    return entry.TypeName;
            }

            string typeName = rsManager.GetTypeNameByGuid(structureGuid) ??
                              shaderTypeManager.GetTypeNameByGuid(structureGuid);

            if (!string.IsNullOrEmpty(typeName))
            {
                return typeName;
            }

            return null;
        }

        public static string GetGuidByStructName(string structName)
        {
            if (_typeCache.TryGetValue(structName, out var cached))
                return cached.Guid;

            string guid = rsManager.GetGuidByStructName(structName) ??
                          shaderTypeManager.GetGuidByStructName(structName);

            if (!string.IsNullOrEmpty(guid))
            {
                string typeName = GetTypeNameByGuid(guid);
                if (!string.IsNullOrEmpty(typeName))
                    _typeCache[structName] = (typeName, guid);

                return guid;
            }

            return null;
        }

        public static string GetTypeNameByStructName(string structName)
        {
            string guid = GetGuidByStructName(structName);
            if (!string.IsNullOrEmpty(guid))
            {
                return GetTypeNameByGuid(guid);
            }

            return null;
        }

        public static void RegisterTypeMapping(string structureGuid, string structName, string mappedTypeName)
        {
            _typeCache[structName] = (mappedTypeName, structureGuid);
        }

        public static void Clear()
        {
            _typeCache.Clear();
        }
    }
}
