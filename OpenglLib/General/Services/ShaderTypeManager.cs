using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class ShaderTypeManager : IService
    {
        private ConcurrentDictionary<string, Dictionary<string, ShaderTypeInfo>> _typesByName = new ConcurrentDictionary<string, Dictionary<string, ShaderTypeInfo>>();
        private ConcurrentDictionary<string, ShaderTypeInfo> _typesByHash = new ConcurrentDictionary<string, ShaderTypeInfo>();

        public string OutputDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "GeneratedCode");

        public Task InitializeAsync()
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }
            return Task.CompletedTask;
        }

        public ShaderTypeInfo RegisterStructType(GlslStructModel structModel, string sourcePath)
        {
            string contentHash = CalculateStructureHash(structModel);

            if (_typesByHash.TryGetValue(contentHash, out var existingInfo))
            {
                return existingInfo;
            }

            var rsManager = ServiceHub.Get<RSManager>();
            var rsStructInfo = rsManager.GetStructTypeInfo(structModel.Name, sourcePath);

            string structureGuid;
            if (rsStructInfo != null)
            {
                structureGuid = rsStructInfo.StructureGuid;
            }
            else
            {
                structureGuid = !string.IsNullOrEmpty(structModel.TypeMappingId)
                    ? structModel.TypeMappingId
                    : Guid.NewGuid().ToString();
            }

            string typeName = GetUniqueTypeName(structModel.Name);
            var typeInfo = new ShaderTypeInfo
            {
                OriginalName = structModel.Name,
                GeneratedTypeName = typeName,
                FilePath = sourcePath,
                IsGenerated = false,
                ContentHash = contentHash,
                Model = structModel,
                StructureGuid = structureGuid
            };

            GlslTypeMapper.RegisterTypeMapping(structureGuid, structModel.Name, typeName);

            if (!_typesByName.TryGetValue(structModel.Name, out var typesDict))
            {
                typesDict = new Dictionary<string, ShaderTypeInfo>();
                _typesByName[structModel.Name] = typesDict;
            }

            typesDict[sourcePath] = typeInfo;
            _typesByHash[contentHash] = typeInfo;

            return typeInfo;
        }

        public ShaderTypeInfo RegisterUniformBlockType(UniformBlockModel blockModel, string sourcePath)
        {
            string contentHash = CalculateUniformBlockHash(blockModel);

            if (_typesByHash.TryGetValue(contentHash, out var existingInfo))
            {
                return existingInfo;
            }

            string typeName = GetUniqueTypeName(blockModel.Name);
            var typeInfo = new ShaderTypeInfo
            {
                OriginalName = blockModel.Name,
                GeneratedTypeName = typeName,
                FilePath = sourcePath,
                IsGenerated = false,
                ContentHash = contentHash,
                Model = blockModel
            };

            if (!_typesByName.TryGetValue(blockModel.Name, out var typesDict))
            {
                typesDict = new Dictionary<string, ShaderTypeInfo>();
                _typesByName[blockModel.Name] = typesDict;
            }

            typesDict[sourcePath] = typeInfo;
            _typesByHash[contentHash] = typeInfo;

            return typeInfo;
        }

        public ShaderTypeInfo GetShaderTypeInfo(string typeName, string sourcePath)
        {
            if (_typesByName.TryGetValue(typeName, out var typesDict))
            {
                if (typesDict.TryGetValue(sourcePath, out var typeInfo))
                {
                    return typeInfo;
                }

                if (typesDict.Count > 0)
                {
                    return typesDict.Values.First();
                }
            }

            return null;
        }

        public void EnsureTypeGenerated(ShaderTypeInfo typeInfo)
        {
            if (typeInfo.IsGenerated)
            {
                return;
            }

            if (typeInfo.Model is GlslStructModel structModel)
            {
                HashSet<string> generatedTypes = new HashSet<string>();
                HashSet<string> processingTypes = new HashSet<string>();

                GenerateStructTypeRecursively(typeInfo, structModel, generatedTypes, processingTypes);
            }
            else if (typeInfo.Model is UniformBlockModel blockModel)
            {
                GenerateUniformBlockType(typeInfo, blockModel);
            }

            typeInfo.IsGenerated = true;
        }

        private void GenerateStructTypeRecursively(ShaderTypeInfo typeInfo, GlslStructModel structModel,
                                                 HashSet<string> generatedTypes, HashSet<string> processingTypes)
        {
            if (generatedTypes.Contains(structModel.Name))
                return;

            if (processingTypes.Contains(structModel.Name))
            {
                throw new Exception($"Circular dependency detected for structure {structModel.Name} in {typeInfo.FilePath}");
            }

            processingTypes.Add(structModel.Name);

            foreach (var field in structModel.Fields)
            {
                if (GlslParser.IsCustomType(field.Type, field.Type) && !GlslParser.IsGlslBaseType(field.Type))
                {
                    var fieldTypeInfo = GetShaderTypeInfo(field.Type, typeInfo.FilePath);

                    if (fieldTypeInfo != null)
                    {
                        if (!fieldTypeInfo.IsGenerated && fieldTypeInfo.Model is GlslStructModel fieldStructModel)
                        {
                            GenerateStructTypeRecursively(fieldTypeInfo, fieldStructModel, generatedTypes, processingTypes);
                        }

                        field.CSharpTypeName = fieldTypeInfo.GeneratedTypeName;
                        field.TypeMappingId = fieldTypeInfo.StructureGuid;
                    }
                    else
                    {
                        var rsManager = ServiceHub.Get<RSManager>();
                        var rsStructInfo = rsManager.GetStructTypeInfo(field.Type, typeInfo.FilePath);

                        if (rsStructInfo != null)
                        {
                            field.CSharpTypeName = rsStructInfo.GeneratedTypeName;
                            field.TypeMappingId = rsStructInfo.StructureGuid;
                        }
                        else
                        {
                            DebLogger.Warn($"Type {field.Type} not found for field in {structModel.Name}");
                        }
                    }
                }
            }

            processingTypes.Remove(structModel.Name);
            GenerateStructType(typeInfo, structModel);
            generatedTypes.Add(structModel.Name);
        }

        private void GenerateStructType(ShaderTypeInfo typeInfo, GlslStructModel structModel)
        {
            try
            {
                var modifiedStructModel = new GlslStructModel
                {
                    Name = typeInfo.GeneratedTypeName,
                    CSharpTypeName = typeInfo.GeneratedTypeName,
                    Fields = structModel.Fields,
                    Attributes = structModel.Attributes,
                    FullText = structModel.FullText.Replace(structModel.Name, typeInfo.GeneratedTypeName),
                    TypeMappingId = typeInfo.StructureGuid
                };

                string modelCode = GlslStructGenerator.GenerateModelClass(modifiedStructModel, typeInfo.StructureGuid);
                string filePath = Path.Combine(OutputDirectory, $"GlslStruct.{typeInfo.GeneratedTypeName}.g.cs");
                File.WriteAllText(filePath, modelCode, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error generating struct {typeInfo.OriginalName}: {ex.Message}");
            }
        }
   
        private void GenerateUniformBlockType(ShaderTypeInfo typeInfo, UniformBlockModel blockModel)
        {
            try
            {
                var modifiedBlockModel = new UniformBlockModel
                {
                    Name = typeInfo.GeneratedTypeName,
                    CSharpTypeName = typeInfo.GeneratedTypeName,
                    Fields = blockModel.Fields,
                    Attributes = blockModel.Attributes,
                    Binding = blockModel.Binding,
                    FullText = blockModel.FullText.Replace(blockModel.Name, typeInfo.GeneratedTypeName),
                    InstanceName = blockModel.InstanceName,
                    UniformBlockType = blockModel.UniformBlockType
                };

                List<GlslStructModel> relatedStructures = new List<GlslStructModel>();
                GlslsUBOGenerator.GenerateUniformBlockClass(
                    modifiedBlockModel,
                    OutputDirectory,
                    null,
                    relatedStructures);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error generating UBO {typeInfo.OriginalName}: {ex.Message}");
            }
        }

        private string GetUniqueTypeName(string baseName)
        {
            string suffix = Guid.NewGuid().ToString().Substring(0, 4);
            return $"{baseName}_{suffix}";
        }

        private string CalculateStructureHash(GlslStructModel structModel)
        {
            using (var md5 = MD5.Create())
            {
                var builder = new StringBuilder();
                builder.Append(structModel.Name);

                foreach (var field in structModel.Fields)
                {
                    builder.Append(field.Type);
                    builder.Append(field.Name);
                    if (field.ArraySize.HasValue)
                    {
                        builder.Append(field.ArraySize.Value);
                    }
                }

                byte[] inputBytes = Encoding.UTF8.GetBytes(builder.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private string CalculateUniformBlockHash(UniformBlockModel blockModel)
        {
            using (var md5 = MD5.Create())
            {
                var builder = new StringBuilder();
                builder.Append(blockModel.Name);
                builder.Append(blockModel.UniformBlockType);

                foreach (var field in blockModel.Fields)
                {
                    builder.Append(field.Type);
                    builder.Append(field.Name);
                    if (field.ArraySize.HasValue)
                    {
                        builder.Append(field.ArraySize.Value);
                    }
                }

                byte[] inputBytes = Encoding.UTF8.GetBytes(builder.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public void UpdateTypeReferences(List<UniformModel> uniforms, List<UniformBlockModel> uniformBlocks, List<RSFileInfo> rsFiles)
        {
            var rsManager = ServiceHub.Get<RSManager>();
             
            foreach (var uniform in uniforms)
            {
                bool typeFoundInRS = false;
                foreach (var rsFile in rsFiles)
                {
                    var matchingStruct = rsFile.Structures.FirstOrDefault(s => s.Name == uniform.Type);
                    if (matchingStruct != null)
                    {
                        var structInfo = rsManager.GetStructTypeInfo(matchingStruct.Name, rsFile.SourcePath);
                        if (structInfo != null)
                        {
                            uniform.CSharpTypeName = structInfo.GeneratedTypeName;
                            typeFoundInRS = true;
                            break;
                        }
                    }
                }
                if (!typeFoundInRS && GlslParser.IsCustomType(uniform.CSharpTypeName, uniform.Type))
                {
                    var typeInfo = GetShaderTypeInfo(uniform.Type, uniform.FullText);
                    if (typeInfo != null)
                    {
                        uniform.CSharpTypeName = typeInfo.GeneratedTypeName;
                    }
                }
            }
            foreach (var block in uniformBlocks)
            {
                bool typeFoundInRS = false;
                foreach (var rsFile in rsFiles)
                {
                    var matchingBlock = rsFile.UniformBlocks.FirstOrDefault(b => b.Name == block.Name);
                    if (matchingBlock != null)
                    {
                        var blockInfo = rsManager.GetUBOTypeInfo(matchingBlock.Name, rsFile.SourcePath);
                        if (blockInfo != null)
                        {
                            block.CSharpTypeName = blockInfo.GeneratedTypeName;
                            typeFoundInRS = true;
                            break;
                        }
                    }
                }
                if (!typeFoundInRS)
                {
                    var typeInfo = GetShaderTypeInfo(block.Name, block.FullText);
                    if (typeInfo != null)
                    {
                        block.CSharpTypeName = typeInfo.GeneratedTypeName;
                    }
                }
            }
        }

        public string GetTypeNameByGuid(string structureGuid)
        {
            foreach (var typeInfo in _typesByHash.Values)
            {
                if (typeInfo.StructureGuid == structureGuid)
                {
                    return typeInfo.GeneratedTypeName;
                }
            }
            return null;
        }

        public string GetGuidByStructName(string structName)
        {
            if (_typesByName.TryGetValue(structName, out var typesDict) && typesDict.Count > 0)
            {
                return typesDict.Values.FirstOrDefault()?.StructureGuid;
            }
            return null;
        }
    }

    public class ShaderTypeInfo : BaseTypeInfo
    {
        public string ContentHash { get; set; } = string.Empty;
        /// <summary>
        /// GlslStructModel или UniformBlockModel
        /// </summary>
        public object Model { get; set; }
    }

}
