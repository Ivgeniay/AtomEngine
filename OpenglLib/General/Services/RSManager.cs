using System.Collections.Concurrent;
using System.Security.Cryptography;
using AtomEngine;
using EngineLib;
using System.Text;

namespace OpenglLib
{
    public class RSManager : IService
    {
        private ConcurrentDictionary<string, RSFileRegistration> _rsFileRegistrations = new ConcurrentDictionary<string, RSFileRegistration>();
        private ConcurrentDictionary<string, Dictionary<string, RSStructTypeInfo>> _structuresByName = new ConcurrentDictionary<string, Dictionary<string, RSStructTypeInfo>>();
        private ConcurrentDictionary<string, Dictionary<string, RSUBOTypeInfo>> _ubosByName = new ConcurrentDictionary<string, Dictionary<string, RSUBOTypeInfo>>();
        private ConcurrentDictionary<string, RSFileInfo> _processedRSFiles = new ConcurrentDictionary<string, RSFileInfo>();

        
        public string OutputDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "GeneratedCode");

        public Task InitializeAsync()
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }
            return Task.CompletedTask;
        }

        public RSFileRegistration RegisterRSFile(string path, string content)
        {
            string fileHash = CalculateFileHash(content);

            if (_rsFileRegistrations.TryGetValue(path, out var existingRegistration))
            {
                if (existingRegistration.FileHash == fileHash)
                {
                    return existingRegistration;
                }
            }

            var rsFileInfo = RSParser.ParseContent(content, path);

            var registration = new RSFileRegistration
            {
                FilePath = path,
                FileHash = fileHash,
                SourceFolder = rsFileInfo.SourceFolder,
                InterfaceName = rsFileInfo.InterfaceName,
                ComponentName = rsFileInfo.ComponentName,
                SystemName = rsFileInfo.SystemName,
                RequiredComponents = rsFileInfo.RequiredComponent
            };

            foreach (var structure in rsFileInfo.Structures)
            {
                string structureGuid = GlslTypeMapper.TryFindStructureGuid(structure.Name, path);

                string generatedTypeName = GetTypeNameForRSFile(structure.Name, path);
                var structInfo = new RSStructTypeInfo
                {
                    OriginalName = structure.Name,
                    GeneratedTypeName = generatedTypeName,
                    FilePath = path,
                    IsGenerated = false,
                    StructureGuid = structureGuid,
                };

                GlslTypeMapper.RegisterTypeMapping(structureGuid, structure.Name, generatedTypeName);

                registration.Structures[structure.Name] = structInfo;

                if (!_structuresByName.TryGetValue(structure.Name, out var structDict))
                {
                    structDict = new Dictionary<string, RSStructTypeInfo>();
                    _structuresByName[structure.Name] = structDict;
                }
                structDict[path] = structInfo;
            }

            foreach (var ubo in rsFileInfo.UniformBlocks)
            {
                string generatedTypeName = GetTypeNameForRSFile(ubo.Name, path);
                var uboInfo = new RSUBOTypeInfo
                {
                    OriginalName = ubo.Name,
                    GeneratedTypeName = generatedTypeName,
                    FilePath = path,
                    IsGenerated = false
                };

                registration.UniformBlocks[ubo.Name] = uboInfo;

                if (!_ubosByName.TryGetValue(ubo.Name, out var uboDict))
                {
                    uboDict = new Dictionary<string, RSUBOTypeInfo>();
                    _ubosByName[ubo.Name] = uboDict;
                }
                uboDict[path] = uboInfo;
            }

            _rsFileRegistrations[path] = registration;
            return registration;
        }

        public void CacheRSInstance(RSFileInfo processedRSFile)
        {
            _processedRSFiles[processedRSFile.SourcePath] = processedRSFile;
        }

        public async Task GenerateComponentsFromRSFile(RSFileInfo processedRSFile)
        {
            if (_rsFileRegistrations.TryGetValue(processedRSFile.SourcePath, out var registration))
            {
                await GenerateRSComponents(registration);
            }
        }

        public RSStructTypeInfo GetStructTypeInfo(string structName, string rsFilePath)
        {
            if (_rsFileRegistrations.TryGetValue(rsFilePath, out var registration))
            {
                if (registration.Structures.TryGetValue(structName, out var structInfo))
                {
                    return structInfo;
                }
            }

            if (_structuresByName.TryGetValue(structName, out var structDict) && structDict.Count > 0)
            {
                return structDict.Values.FirstOrDefault();
            }

            return null;
        }

        public RSUBOTypeInfo GetUBOTypeInfo(string uboName, string rsFilePath)
        {
            if (_rsFileRegistrations.TryGetValue(rsFilePath, out var registration))
            {
                if (registration.UniformBlocks.TryGetValue(uboName, out var uboInfo))
                {
                    return uboInfo;
                }
            }

            if (_ubosByName.TryGetValue(uboName, out var uboDict) && uboDict.Count > 0)
            {
                return uboDict.Values.FirstOrDefault();
            }

            return null;
        }

        public string GetTypeNameForRSFile(string baseName, string rsFilePath)
        {
            string pathHash = CalculatePathHash(rsFilePath);
            return $"{baseName}RS_{pathHash.Substring(0, 4)}";
        }

        public bool HasRSFile(string path)
        {
            return _rsFileRegistrations.ContainsKey(path);
        }

        public bool HasRSFileChanged(string path, string content)
        {
            if (!_rsFileRegistrations.TryGetValue(path, out var registration))
            {
                throw new KeyNotFoundException($"RS file not registered: {path}");
            }

            string newHash = CalculateFileHash(content);
            return registration.FileHash != newHash;
        }

        private string CalculateFileHash(string content)
        {
            using (var md5 = MD5.Create())
            {
                byte[] contentBytes = Encoding.UTF8.GetBytes(content);
                byte[] hashBytes = md5.ComputeHash(contentBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private string CalculatePathHash(string path)
        {
            using (var md5 = MD5.Create())
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(path);
                byte[] hashBytes = md5.ComputeHash(pathBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public async Task EnsureTypesGenerated(string rsFilePath)
        {
            if (!_rsFileRegistrations.TryGetValue(rsFilePath, out var registration))
            {
                throw new KeyNotFoundException($"RS file not registered: {rsFilePath}");
            }
            foreach (var structInfo in registration.Structures.Values)
            {
                if (!structInfo.IsGenerated)
                {
                    GenerateStructType(rsFilePath, structInfo, registration);
                    structInfo.IsGenerated = true;
                }
            }
            foreach (var uboInfo in registration.UniformBlocks.Values)
            {
                if (!uboInfo.IsGenerated)
                {
                    GenerateUBOType(rsFilePath, uboInfo, registration);
                    uboInfo.IsGenerated = true;
                }
            }
            //await GenerateRSComponents(registration);
        }

        private void GenerateStructType(string rsFilePath, RSStructTypeInfo structInfo, RSFileRegistration registration)
        {
            try
            {
                string rsContent = FileLoader.LoadFile(rsFilePath);
                var rsFileInfo = RSParser.ParseContent(rsContent, rsFilePath);

                var structModel = rsFileInfo.Structures.FirstOrDefault(s => s.Name == structInfo.OriginalName);
                if (structModel == null)
                {
                    DebLogger.Error($"Structure {structInfo.OriginalName} not found in {rsFilePath}");
                    return;
                }

                var modifiedStructModel = new GlslStructModel
                {
                    Name = structInfo.GeneratedTypeName,
                    Fields = structModel.Fields,
                    Attributes = structModel.Attributes,
                    FullText = structModel.FullText.Replace(structInfo.OriginalName, structInfo.GeneratedTypeName)
                };
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }

                string modelCode = GlslStructGenerator.GenerateModelClass(modifiedStructModel);
                string filePath = Path.Combine(OutputDirectory, $"GlslStruct.{structInfo.GeneratedTypeName}.g.cs");
                File.WriteAllText(filePath, modelCode, Encoding.UTF8);

                structInfo.IsGenerated = true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error generating struct {structInfo.OriginalName}: {ex.Message}");
            }
        }

        private void GenerateUBOType(string rsFilePath, RSUBOTypeInfo uboInfo, RSFileRegistration registration)
        {
            try
            {
                string rsContent = FileLoader.LoadFile(rsFilePath);
                var rsFileInfo = RSParser.ParseContent(rsContent, rsFilePath);

                var uboModel = rsFileInfo.UniformBlocks.FirstOrDefault(u => u.Name == uboInfo.OriginalName);
                if (uboModel == null)
                {
                    return;
                }

                var modifiedUboModel = new UniformBlockModel
                {
                    Name = uboInfo.GeneratedTypeName,
                    CSharpTypeName = uboInfo.GeneratedTypeName,
                    Fields = uboModel.Fields,
                    Attributes = uboModel.Attributes,
                    Binding = uboModel.Binding,
                    FullText = uboModel.FullText.Replace(uboInfo.OriginalName, uboInfo.GeneratedTypeName),
                    InstanceName = uboModel.InstanceName,
                    UniformBlockType = uboModel.UniformBlockType
                };

                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }

                GlslsUBOGenerator.GenerateUniformBlockClass(
                    modifiedUboModel,
                    OutputDirectory,
                    rsFilePath,
                    rsFileInfo.Structures);

                uboInfo.IsGenerated = true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error generating UBO {uboInfo.OriginalName}: {ex.Message}");
            }
        }

        

        private async Task GenerateRSComponents(RSFileRegistration registration)
        {
            try
            {
                string rsFilePath = registration.FilePath;

                RSFileInfo rsFileInfo;
                if (!_processedRSFiles.TryGetValue(rsFilePath, out rsFileInfo))
                {
                    string rsContent = FileLoader.LoadFile(rsFilePath);
                    rsFileInfo = RSParser.ParseContent(rsContent, rsFilePath);
                }

                UpdateRSFileTypesWithGeneratedNames(rsFileInfo);
                if (!Directory.Exists(OutputDirectory))  Directory.CreateDirectory(OutputDirectory);

                string interfaceCode = InterfaceGenerator.GenerateInterface(rsFileInfo);
                string interfacePath = Path.Combine(OutputDirectory, $"{registration.InterfaceName}.cs");
                await Task.Delay(200);
                File.WriteAllText(interfacePath, interfaceCode);

                string componentCode = ComponentGenerator.GenerateComponentTemplate(rsFileInfo);
                string componentPath = Path.Combine(OutputDirectory, $"{registration.ComponentName}.cs");
                await Task.Delay(200);
                File.WriteAllText(componentPath, componentCode);

                string systemCode = RenderSystemGenerator.GenerateRenderSystemTemplate(rsFileInfo);
                string systemPath = Path.Combine(OutputDirectory, $"{registration.SystemName}.cs");
                await Task.Delay(200);
                File.WriteAllText(systemPath, systemCode);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error generating components for {registration.FilePath}: {ex.Message}");
            }
        }

        public void UpdateRSFileTypesWithGeneratedNames(RSFileInfo rsFileInfo)
        {
            if (!_rsFileRegistrations.TryGetValue(rsFileInfo.SourcePath, out var registration))
            {
                return;
            }

            //foreach (var structure in rsFileInfo.Structures)
            //{
            //    if (registration.Structures.TryGetValue(structure.Name, out var structInfo))
            //    {
            //        structure.CSharpTypeName = structInfo.GeneratedTypeName;
            //    }
            //}

            foreach (var structure in rsFileInfo.Structures)
            {
                if (string.IsNullOrEmpty(structure.TypeMappingId))
                {
                    structure.TypeMappingId = GlslTypeMapper.TryFindStructureGuid(structure.Name, rsFileInfo.SourcePath);
                }
                foreach (var field in structure.Fields)
                {
                    if (GlslParser.IsCustomType(field.Type, field.Type))
                    {
                        field.TypeMappingId = GlslTypeMapper.TryFindStructureGuid(field.Type, rsFileInfo.SourcePath);

                        if (!string.IsNullOrEmpty(field.TypeMappingId))
                        {
                            string mappedTypeName = GlslTypeMapper.GetTypeNameByGuid(field.TypeMappingId);
                            if (!string.IsNullOrEmpty(mappedTypeName))
                            {
                                field.CSharpTypeName = mappedTypeName;
                            }
                        }
                    }
                }
            }

            foreach (var ubo in rsFileInfo.UniformBlocks)
            {
                if (registration.UniformBlocks.TryGetValue(ubo.Name, out var uboInfo))
                {
                    ubo.CSharpTypeName = uboInfo.GeneratedTypeName;
                }
            }

            foreach (var uniform in rsFileInfo.Uniforms)
            {
                bool isCustomType = false;

                foreach (var struct_ in rsFileInfo.Structures)
                {
                    if (uniform.Type == struct_.Name)
                    {
                        isCustomType = true;
                        if (registration.Structures.TryGetValue(struct_.Name, out var structInfo))
                        {
                            uniform.CSharpTypeName = structInfo.GeneratedTypeName;
                        }
                        break;
                    }
                }

                if (!isCustomType)
                {
                    uniform.CSharpTypeName = GlslParser.MapGlslTypeToCSharp(uniform.Type);
                }
            }

            foreach (var structInstance in rsFileInfo.StructureInstances)
            {
                if (!structInstance.IsUniform)
                {
                    if (structInstance.OriginalInstanceName != null &&
                        structInstance.OriginalInstanceName != structInstance.InstanceName)
                    {
                        //DebLogger.Info($"В RS-файле используется переименованный инстанс: {structInstance.OriginalInstanceName} -> {structInstance.InstanceName}");
                    }

                    var structName = structInstance.Structure.Name;
                    if (registration.Structures.TryGetValue(structName, out var structInfo))
                    {
                        structInstance.Structure.CSharpTypeName = structInfo.GeneratedTypeName;
                    }
                }
            }
        }

    }

    public class RSFileRegistration
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileHash { get; set; } = string.Empty;
        public string SourceFolder { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public Dictionary<string, RSStructTypeInfo> Structures { get; set; } = new Dictionary<string, RSStructTypeInfo>();
        public Dictionary<string, RSUBOTypeInfo> UniformBlocks { get; set; } = new Dictionary<string, RSUBOTypeInfo>();
        public List<string> RequiredComponents { get; set; } = new List<string>();
    }

    public class RSStructTypeInfo
    {
        public string StructureGuid { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public string GeneratedTypeName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsGenerated { get; set; }
    }

    public class RSUBOTypeInfo
    {
        public string OriginalName { get; set; } = string.Empty;
        public string GeneratedTypeName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsGenerated { get; set; }
    }
}
