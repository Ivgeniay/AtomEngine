using AtomEngine;
using EngineLib;
using System.Collections.Concurrent;

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
                        //await rsManager.EnsureTypesGenerated(rsFile.SourcePath);
                        rsManager.UpdateRSFileTypesWithGeneratedNames(rsFile);
                        //await rsManager.GenerateComponentsFromRSFile(rsFile);

                        foreach (var structInfo in registration.Structures.Values)
                        {
                            processedStructures.Add(structInfo.OriginalName);
                        }

                        foreach (var uboInfo in registration.UniformBlocks.Values)
                        {
                            processedUBOs.Add(uboInfo.OriginalName);
                        }
                    }

                    ShaderStructureInstanceProcessor.PropagateRSTypesToShaderModel(shader);
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
                    shaderModel: shader,
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


    public static class ShaderStructureInstanceProcessor
    {
        private const string VERTEX_SUFFIX = "_vt";
        private const string FRAGMENT_SUFFIX = "_fg";

        public static void ProcessStructureInstances(GlslShaderModel shaderModel)
        {
            if (shaderModel == null) return;

            var instanceMap = new Dictionary<string, List<InstanceInfo>>();
            CollectInstances(shaderModel, instanceMap);
            ProcessDuplicateInstances(instanceMap);
            ApplyChangesToModel(shaderModel, instanceMap);
        }

        public static void PropagateRSTypesToShaderModel(GlslShaderModel shaderModel)
        {
            List<RSFileInfo> rsFiles = shaderModel.RSFiles;

            if (shaderModel == null || rsFiles == null || rsFiles.Count == 0)
                return;

            var typeMap = new Dictionary<string, string>();

            foreach (var rsFile in rsFiles)
            {
                foreach (var structure in rsFile.Structures)
                {
                    if (!string.IsNullOrEmpty(structure.CSharpTypeName))
                    {
                        typeMap[structure.Name] = structure.CSharpTypeName;
                    }
                }
            }

            if (shaderModel.Vertex != null)
            {
                UpdateStructureTypes(shaderModel.Vertex.Structures, typeMap);
                UpdateStructureInstanceTypes(shaderModel.Vertex.StructureInstances, typeMap);
            }

            if (shaderModel.Fragment != null)
            {
                UpdateStructureTypes(shaderModel.Fragment.Structures, typeMap);
                UpdateStructureInstanceTypes(shaderModel.Fragment.StructureInstances, typeMap);
            }
        }

        public static void SyncRSFilesWithShaderModel(GlslShaderModel shaderModel)
        {
            if (shaderModel == null || shaderModel.RSFiles == null || shaderModel.RSFiles.Count == 0)
                return;

            foreach (var rsFile in shaderModel.RSFiles)
            {
                foreach (var rsInstance in rsFile.StructureInstances)
                {
                    if (rsInstance.IsUniform)
                        continue;

                    if (shaderModel.Vertex != null)
                    {
                        var vertexInstance = shaderModel.Vertex.StructureInstances
                            .FirstOrDefault(i => !i.IsUniform && i.OriginalInstanceName == rsInstance.InstanceName);

                        if (vertexInstance != null)
                        {
                            rsInstance.OriginalInstanceName = rsInstance.InstanceName;
                            rsInstance.InstanceName = vertexInstance.InstanceName;
                            continue;
                        }
                    }

                    if (shaderModel.Fragment != null)
                    {
                        var fragmentInstance = shaderModel.Fragment.StructureInstances
                            .FirstOrDefault(i => !i.IsUniform && i.OriginalInstanceName == rsInstance.InstanceName);

                        if (fragmentInstance != null)
                        {
                            rsInstance.OriginalInstanceName = rsInstance.InstanceName;
                            rsInstance.InstanceName = fragmentInstance.InstanceName;
                        }
                    }
                }
            }
        }

        private static void UpdateStructureTypes(List<GlslStructModel> structures, Dictionary<string, string> typeMap)
        {
            foreach (var structure in structures)
            {
                if (typeMap.TryGetValue(structure.Name, out var csharpType))
                {
                    structure.CSharpTypeName = csharpType;
                }

                UpdateStructureFields(structure.Fields, typeMap);
            }
        }

        private static void UpdateStructureFields(List<GlslStructFieldModel> fields, Dictionary<string, string> typeMap)
        {
            foreach (var field in fields)
            {
                if (typeMap.TryGetValue(field.Type, out var csharpType))
                {
                    field.CSharpTypeName = csharpType;
                }
            }
        }

        private static void UpdateStructureInstanceTypes(List<GlslStructInstance> instances, Dictionary<string, string> typeMap)
        {
            foreach (var instance in instances)
            {
                if (instance.Structure != null)
                {
                    if (typeMap.TryGetValue(instance.Structure.Name, out var csharpType))
                    {
                        instance.Structure.CSharpTypeName = csharpType;
                    }

                    UpdateStructureFields(instance.Structure.Fields, typeMap);
                }
            }
        }




        private static void CollectInstances(GlslShaderModel shaderModel, Dictionary<string, List<InstanceInfo>> instanceMap)
        {
            if (shaderModel.Vertex != null)
            {
                foreach (var instance in shaderModel.Vertex.StructureInstances)
                {
                    if (!instance.IsUniform)
                    {
                        if (!instanceMap.ContainsKey(instance.InstanceName))
                        {
                            instanceMap[instance.InstanceName] = new List<InstanceInfo>();
                        }

                        instanceMap[instance.InstanceName].Add(new InstanceInfo
                        {
                            Instance = instance,
                            IsVertex = true,
                            StructureHash = CalculateStructureHash(instance.Structure)
                        });

                        instance.OriginalInstanceName = instance.InstanceName;
                    }
                }
            }

            if (shaderModel.Fragment != null)
            {
                foreach (var instance in shaderModel.Fragment.StructureInstances)
                {
                    if (!instance.IsUniform)
                    {
                        if (!instanceMap.ContainsKey(instance.InstanceName))
                        {
                            instanceMap[instance.InstanceName] = new List<InstanceInfo>();
                        }

                        instanceMap[instance.InstanceName].Add(new InstanceInfo
                        {
                            Instance = instance,
                            IsVertex = false,
                            StructureHash = CalculateStructureHash(instance.Structure)
                        });

                        instance.OriginalInstanceName = instance.InstanceName;
                    }
                }
            }
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

                bool areStructuresIdentical = instances.Select(i => i.StructureHash).Distinct().Count() == 1;

                if (areStructuresIdentical)
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
                else
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
            }
        }

        private static void ApplyChangesToModel(GlslShaderModel shaderModel, Dictionary<string, List<InstanceInfo>> instanceMap)
        {
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
        private static readonly ConcurrentDictionary<string, string> _guidToTypeNameMap =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> _structNameToGuidMap =
            new ConcurrentDictionary<string, string>();

        public static void RegisterTypeMapping(string structureGuid, string structName, string mappedTypeName)
        {
            _guidToTypeNameMap[structureGuid] = mappedTypeName;
            _structNameToGuidMap[structName] = structureGuid;

            DebLogger.Info($"Registered type mapping: {structName} ({structureGuid}) -> {mappedTypeName}");
        }

        public static string GetTypeNameByGuid(string structureGuid)
        {
            if (_guidToTypeNameMap.TryGetValue(structureGuid, out var typeName))
                return typeName;

            return null;
        }

        public static string GetGuidByStructName(string structName)
        {
            if (_structNameToGuidMap.TryGetValue(structName, out var guid))
                return guid;

            return null;
        }

        public static string GetTypeNameByStructName(string structName)
        {
            string guid = GetGuidByStructName(structName);
            if (guid != null)
            {
                return GetTypeNameByGuid(guid);
            }

            return null;
        }

        public static string TryFindStructureGuid(string structureName, string sourcePath = null)
        {
            string guid = GetGuidByStructName(structureName);
            if (!string.IsNullOrEmpty(guid))
                return guid;

            var rsManager = ServiceHub.Get<RSManager>();
            var rsStructInfo = rsManager.GetStructTypeInfo(structureName, sourcePath);
            if (rsStructInfo != null && !string.IsNullOrEmpty(rsStructInfo.StructureGuid))
                return rsStructInfo.StructureGuid;

            var shaderTypeManager = ServiceHub.Get<ShaderTypeManager>();
            var shaderTypeInfo = shaderTypeManager.GetShaderTypeInfo(structureName, sourcePath);
            if (shaderTypeInfo != null && !string.IsNullOrEmpty(shaderTypeInfo.StructureGuid))
                return shaderTypeInfo.StructureGuid;

            return Guid.NewGuid().ToString();
        }

        public static void Clear()
        {
            _guidToTypeNameMap.Clear();
            _structNameToGuidMap.Clear();
        }
    }
}
