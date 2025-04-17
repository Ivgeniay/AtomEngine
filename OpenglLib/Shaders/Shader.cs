using AtomEngine.RenderEntity;
using Newtonsoft.Json;
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using System.Text;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class Shader : ShaderBase
    {
        public readonly GL _gl;

        protected readonly ShaderTextureManager _shaderTextureManager;
        protected readonly BindingPointService _bindingPointService;
        protected readonly UboService _uboService;

        protected readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();
        protected readonly Dictionary<string, uint> _attributeLocations = new Dictionary<string, uint>();
        protected readonly Dictionary<string, UniformInfo> _uniformInfo = new Dictionary<string, UniformInfo>();
        protected readonly List<UniformBlockData> _uniformBlocks = new List<UniformBlockData>();
        protected readonly Dictionary<string, UniformSamplerInfo> _samplerUniforms = new Dictionary<string, UniformSamplerInfo>();

        //protected AutoUBOHub autoUBOHub;

        protected string VertexSource;
        protected string FragmentSource;

        public Shader(GL gl)
        {
            _gl = gl;
            _shaderTextureManager = new ShaderTextureManager(this);
            _bindingPointService = ServiceHub.Get<BindingPointService>();
            _uboService = ServiceHub.Get<UboService>();
        }

        public void SetUpShader(string vertexSource = "", string fragmentSource = "")
        {
            vertexSource = string.IsNullOrEmpty(vertexSource) ? VertexSource : vertexSource;
            fragmentSource = string.IsNullOrEmpty(fragmentSource) ? FragmentSource : fragmentSource;

            Result<uint, Error> mb_vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint vertexShader = mb_vertexShader.Unwrap();

            Result<uint, Error> mb_fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
            uint fragmentShader = mb_fragmentShader.Unwrap();

            handle = _gl.CreateProgram();
            _gl.AttachShader(handle, vertexShader);
            _gl.AttachShader(handle, fragmentShader);
            LinkProgram();
            CleanupResources(vertexShader, fragmentShader);

            CacheAttributes(_gl, handle, _attributeLocations);
            CacheUniforms(_gl, handle, _uniformLocations, _uniformInfo);
            CacheSamplerUniforms(_gl, handle, _samplerUniforms, _uniformLocations, vertexSource, fragmentSource);
            CacheUniformBlocks(_gl, handle, _uniformBlocks);

            foreach(var kvp in _samplerUniforms)
            {
                if (kvp.Value.BindingPoint.HasValue)
                {
                    _shaderTextureManager.ReserveTextureUnit(kvp.Value.Name, kvp.Value.BindingPoint.Value);
                }
            }
            if (_uniformBlocks.Count > 0)
            {
                for(int i = 0;  i < _uniformBlocks.Count; i++)
                {
                    var str = _uniformBlocks[i].ToString();
                    var ubo = _uboService.GetOrCreateUbo(_uniformBlocks[i]);
                    if (!_uniformBlocks[i].BindingPoint.HasValue)
                    {
                        _uniformBlocks[i] = new UniformBlockData(
                            _uniformBlocks[i].Name,
                            _uniformBlocks[i].BlockIndex,
                            _uniformBlocks[i].BlockSize,
                            _uniformBlocks[i].ActiveUniforms,
                            _uniformBlocks[i].Members,
                            _uniformBlocks[i].BindingPoint
                            );
                    }
                }
            }
        }

        public override void Use()
        {
            _gl.UseProgram(handle);
        }

        public override void SetUbo(Dictionary<string, object> uniformValues)
        {
            bool anyFound = false;

            foreach (var kvp in uniformValues)
            {
                foreach(var block in _uniformBlocks)
                {
                    foreach (var member in block.Members)
                    {
                        if (member.Name == kvp.Key)
                        {
                            _uboService.SetUniform(block.Name, kvp.Key, kvp.Value);
                            anyFound = true;
                        }
                    }
                }
            }

            if (anyFound) _uboService.UpdateAll();
        }

        public override void SetUniform(string name, object value)
        {
            if (name == null)
            {
#if DEBUG
                DebLogger.Error("Uniform name cannot be null");
#endif
                return;
            }

            if (!_uniformLocations.TryGetValue(name, out int location))
            {
#if DEBUG
                DebLogger.Error($"Uniform {name} not found in shader program");
#endif
                return;
            }

            if (location < 0)
            {
                foreach (var block in _uniformBlocks)
                {
                    foreach (var member in block.Members)
                    {
                        if (member.Name == name)
                        {
                            _uboService.SetUniform(block.Name, name, value);
                            _uboService.UpdateAll();
                            return;
                        }
                    }
                }

                DebLogger.Error($"Uniform {name} not found in any uniform block");
                return;
            }

            if (_uniformInfo.TryGetValue(name, out var uniInfo))
            {
                switch (uniInfo.Type)
                {
                    case UniformType.Float:
                        _gl.Uniform1(location, Convert.ToSingle(value));
                        return;
                    case UniformType.Int:
                        _gl.Uniform1(location, Convert.ToInt32(value));
                        return;
                    case UniformType.Bool:
                        _gl.Uniform1(location, Convert.ToBoolean(value) ? 1 : 0);
                        return;
                    case UniformType.Double:
                        _gl.Uniform1(location, Convert.ToDouble(value));
                        return;

                    case UniformType.FloatVec2:
                        var vec2 = (Vector2D<float>)value;
                        _gl.Uniform2(location, vec2.X, vec2.Y);
                        return;
                    case UniformType.FloatVec3:
                        var vec3 = (Vector3D<float>)value;
                        _gl.Uniform3(location, vec3.X, vec3.Y, vec3.Z);
                        return;
                    case UniformType.FloatVec4:
                        var vec4 = (Vector4D<float>)value;
                        _gl.Uniform4(location, vec4.X, vec4.Y, vec4.Z, vec4.W);
                        return;

                    case UniformType.IntVec2:
                        var ivec2 = (Vector2D<int>)value;
                        _gl.Uniform2(location, ivec2.X, ivec2.Y);
                        return;
                    case UniformType.IntVec3:
                        var ivec3 = (Vector3D<int>)value;
                        _gl.Uniform3(location, ivec3.X, ivec3.Y, ivec3.Z);
                        return;
                    case UniformType.IntVec4:
                        var ivec4 = (Vector4D<int>)value;
                        _gl.Uniform4(location, ivec4.X, ivec4.Y, ivec4.Z, ivec4.W);
                        return;

                    case UniformType.FloatMat2:
                        unsafe
                        {
                            if (value is Matrix2X2<float> mat2)
                            {
                                _gl.UniformMatrix2(location, 1, false, (float*)&mat2);
                            }
                        }
                        return;
                    case UniformType.FloatMat3:
                        unsafe
                        {
                            if (value is Matrix3X3<float> mat3)
                            {
                                _gl.UniformMatrix3(location, 1, false, (float*)&mat3);
                            }
                        }
                        return;
                    case UniformType.FloatMat4:
                        unsafe
                        {
                            if (value is Matrix4X4<float> mat4)
                            {
                                _gl.UniformMatrix4(location, 1, false, (float*)&mat4);
                            }
                            else if (value is System.Numerics.Matrix4x4 mat4x4)
                            {
                                var silk = mat4x4.ToSilk();
                                _gl.UniformMatrix4(location, 1, false, (float*)&silk);
                            }
                        }
                        return;
                    case UniformType.FloatMat2x3:
                        unsafe
                        {
                            if (value is Matrix2X3<float> mat2x3)
                            {
                                _gl.UniformMatrix2x3(location, 1, false, (float*)&mat2x3);
                            }
                        }
                        return;
                    case UniformType.FloatMat2x4:
                        unsafe
                        {
                            if (value is Matrix2X4<float> mat2x4)
                                _gl.UniformMatrix2x4(location, 1, false, (float*)&mat2x4);
                        }
                        return;
                    case UniformType.FloatMat3x2:
                        unsafe
                        {
                            if (value is Matrix3X2<float> mat3x2)
                                _gl.UniformMatrix3x2(location, 1, false, (float*)&mat3x2);

                            else if (value is System.Numerics.Matrix3x2 mat3x2Num)
                            {
                                var silk = mat3x2Num.ToSilk();
                                _gl.UniformMatrix3x2(location, 1, false, (float*)&mat3x2Num);
                            }
                        }
                        return;
                    case UniformType.FloatMat3x4:
                        unsafe
                        {
                            if (value is Matrix3X4<float> mat3x4)
                            {
                                _gl.UniformMatrix3x4(location, 1, false, (float*)&mat3x4);
                            }
                        }
                        return;
                    case UniformType.FloatMat4x2:
                        unsafe
                        {
                            if (value is Matrix4X2<float> mat4x2)
                            {
                                _gl.UniformMatrix4x2(location, 1, false, (float*)&mat4x2);
                            }
                        }
                        return;
                    case UniformType.FloatMat4x3:
                        unsafe
                        {
                            if (value is Matrix4X3<float> mat4x3)
                            {
                                _gl.UniformMatrix4x3(location, 1, false, (float*)&mat4x3);
                            }
                        }
                        return;
                }
            }

            if (_samplerUniforms.TryGetValue(name, out var sampInfo))
            {
                switch(sampInfo.Type)
                {
                    case UniformType.Sampler1D:
                    case UniformType.Sampler2D:
                    case UniformType.Sampler1DArray:
                    case UniformType.Sampler2DArray:
                    case UniformType.Sampler3D:
                    case UniformType.SamplerCube:
                    case UniformType.Sampler1DShadow:
                    case UniformType.Sampler2DShadow:
                    case UniformType.Sampler2DRect:
                    case UniformType.Sampler2DRectShadow:
                    case UniformType.SamplerBuffer:
                    case UniformType.Sampler1DArrayShadow:
                    case UniformType.Sampler2DArrayShadow:
                    case UniformType.SamplerCubeShadow:
                    case UniformType.IntSampler1D:
                    case UniformType.IntSampler2D:
                    case UniformType.IntSampler3D:
                    case UniformType.IntSamplerCube:
                    case UniformType.IntSampler2DRect:
                    case UniformType.IntSampler1DArray:
                    case UniformType.IntSampler2DArray:
                    case UniformType.IntSamplerBuffer:
                    case UniformType.UnsignedIntSampler1D:
                    case UniformType.UnsignedIntSampler2D:
                    case UniformType.UnsignedIntSampler3D:
                    case UniformType.UnsignedIntSamplerCube:
                    case UniformType.UnsignedIntSampler2DRect:
                    case UniformType.UnsignedIntSampler1DArray:
                    case UniformType.UnsignedIntSampler2DArray:
                    case UniformType.UnsignedIntSamplerBuffer:
                    case UniformType.SamplerCubeMapArray:
                    case UniformType.SamplerCubeMapArrayShadow:
                    case UniformType.IntSamplerCubeMapArray:
                    case UniformType.UnsignedIntSamplerCubeMapArray:
                    case UniformType.Sampler2DMultisample:
                    case UniformType.IntSampler2DMultisample:
                    case UniformType.UnsignedIntSampler2DMultisample:
                    case UniformType.Sampler2DMultisampleArray:
                    case UniformType.IntSampler2DMultisampleArray:
                    case UniformType.UnsignedIntSampler2DMultisampleArray:
                        _gl.Uniform1(location, Convert.ToInt32(value));
                        return;
                }
            }
            throw new ArgumentException(
                        $"Unsupported uniform type. Uniform: {name}, Type: {_uniformInfo[name].Type}");
        }

        #region Textures
        public void SetTexture(string uniformName, Texture texture)
        {
            if (texture == null || string.IsNullOrEmpty(uniformName))
            {
#if DEBUG
                DebLogger.Error($"Texture or uniform name cannot be null");
#endif
                return;
            }

            if (!_uniformLocations.TryGetValue(uniformName, out int location))
            {
#if DEBUG
                DebLogger.Error($"Uniform {uniformName} not found in shader program");
#endif
                return;
            }

            Use();
            _shaderTextureManager.BindTexture(uniformName, texture);
        }
        public void SetTexture(int location, Texture texture)
        {
            if (texture == null)
            {
#if DEBUG
                DebLogger.Error($"Texture for uniform at location {location} cannot be null");
#endif
                return;
            }

            if (location < 0)
            {
#if DEBUG
                DebLogger.Error($"Uniform at location for Texture cannot be {location}");
#endif
                return;
            }

            string uniformName = null;
            foreach (var pair in _uniformLocations)
            {
                if (pair.Value == location)
                {
                    uniformName = pair.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(uniformName))
            {
#if DEBUG
                DebLogger.Error($"Uniform domainName cannot be white space or null");
#endif
            }
            else
            {
                SetTexture(uniformName, texture);
            }
        }
        public void SetTextureByDomainName(string name, object value)
        {
            if (name == null)
            {
#if DEBUG
                DebLogger.Error("Uniform name cannot be null");
#endif
                return;
            }
            if (value == null)
            {
#if DEBUG
                DebLogger.Error($"Value for uniform {name} cannot be null");
#endif
                return;
            }
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
#if DEBUG
                DebLogger.Error($"Uniform {name} not found in shader program");
#endif
                return;
            }
            
            switch (_uniformInfo[name].Type)
            {
                case UniformType.Sampler1D:
                case UniformType.Sampler2D:
                case UniformType.Sampler3D:
                case UniformType.SamplerCube:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DShadow:
                case UniformType.SamplerCubeShadow:
                    try
                    {
                        _gl.Uniform1(location, Convert.ToInt32(value));
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        DebLogger.Error($"Failed to convert value for uniform {name}: {ex.Message}");
#endif
                    }
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported uniform type. Uniform: {name}, Type: {_uniformInfo[name].Type}");
            }

        }
        public void ResetTextureUnits()
        {
            _shaderTextureManager.Reset();
        }
        #endregion

        public int GetUniformLocation(string uniformName)
        {
            if (_uniformLocations.TryGetValue(uniformName, out int location)) 
            { 
                return location; 
            }
            return -1;
        }
        
        protected uint GetBlockIndex(string name)
        {
            var uniform = _uniformBlocks.FirstOrDefault(block => block.Name == name);
            return uniform.BlockIndex;
        }

        internal IReadOnlyDictionary<string, UniformInfo> GetAllUniformInfo()
        {
            return _uniformInfo;
        }

        internal IReadOnlyDictionary<string, uint> GetAllAttributeLocations()
        {
            return _attributeLocations;
        }

        private void CleanupResources(uint vertexShader, uint fragmentShader)
        {
            _gl.DetachShader(handle, vertexShader);
            _gl.DetachShader(handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        private Result<uint, Error> CompileShader(ShaderType type, string source)
        {
            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            string infoLog = _gl.GetShaderInfoLog(shader);
            return string.IsNullOrEmpty(infoLog) ?
                new Result<uint, Error>(shader) :
                new Result<uint, Error>(new CompileError($"Error compiling shader of type {type}: {infoLog}"));
        }

        private void LinkProgram()
        {
            _gl.LinkProgram(handle);
            _gl.GetProgram(handle, GLEnum.LinkStatus, out int success);
            if (success == 0)
            { 
                throw new Exception($"Error linking shader program: {_gl.GetProgramInfoLog(handle)}");
            }
        }

        internal static void CacheAttributes(
            GL _gl, 
            uint handle, 
            Dictionary<string, uint> _attributeLocations)
        {
            _gl.GetProgram(handle, GLEnum.ActiveAttributes, out int attributeCount);

            for (uint i = 0; i < attributeCount; i++)
            {
                string attributeName = _gl.GetActiveAttrib(handle, i, out int size, out AttributeType type);
                uint location = (uint)_gl.GetAttribLocation(handle, attributeName);
                _attributeLocations[attributeName] = location;
            }
        }


        internal static void CacheUniforms(
            GL _gl, 
            uint handle, 
            Dictionary<string, int> _uniformLocations,
            Dictionary<string, UniformInfo> _uniformInfo)
        {
            _gl.GetProgram(handle, GLEnum.ActiveUniforms, out int uniformCount);
            Dictionary<string, (int location, int size, UniformType type)> arrayInfo = new Dictionary<string, (int, int, UniformType)>();

            for (int i = 0; i < uniformCount; i++)
            {
                string uniformName = _gl.GetActiveUniform(handle, (uint)i, out int size, out UniformType type);
                int location = _gl.GetUniformLocation(handle, uniformName);

                if (GlslParser.IsSamplerType(type)) continue;
                
                _uniformLocations[uniformName] = location;
                _uniformInfo[uniformName] = new UniformInfo
                {
                    Location = location,
                    Size = size,
                    Type = type,
                    Name = uniformName
                };

                if (location >= 0 && uniformName.EndsWith("[0]"))
                {
                    ProcessArrayElements(uniformName, location, size, type, _uniformLocations, _uniformInfo);
                }
            }
        }

        internal static void ProcessArrayElements(
            string uniformName, 
            int baseLocation, 
            int size, 
            UniformType type,
            Dictionary<string, int> _uniformLocations,
            Dictionary<string, UniformInfo> _uniformInfo)
        {
            if (size <= 1)
                return;

            string baseName;

            if (uniformName.EndsWith("[0]"))
            {
                baseName = uniformName.Substring(0, uniformName.Length - 3);
            }
            else
            {
                int lastOpenBracket = uniformName.LastIndexOf("[0");
                baseName = uniformName.Substring(0, lastOpenBracket + 1); 
            }

            for (int i = 1; i < size; i++)
            {
                string elementName = baseName + "[" + i + "]";
                int elementLocation = baseLocation + i;

                _uniformLocations[elementName] = elementLocation;
                _uniformInfo[elementName] = new UniformInfo
                {
                    Location = elementLocation,
                    Size = 1,
                    Type = type,
                    Name = elementName
                };
            }
        }

        internal static unsafe void CacheUniformBlocks(
            GL _gl, 
            uint handle,
            List<UniformBlockData> _uniformBlocks)
        {
            _gl.GetProgram(handle, GLEnum.ActiveUniformBlocks, out int uniformBlockCount);
            for (uint i = 0; i < uniformBlockCount; i++)
            {
                byte[] nameBuffer = new byte[256];
                uint nameLength = 0;
                fixed (byte* namePtr = nameBuffer)
                {
                    _gl.GetActiveUniformBlockName(handle, i, (uint)nameBuffer.Length, &nameLength, namePtr);
                    string name = Encoding.ASCII.GetString(nameBuffer, 0, (int)nameLength);
                    uint blockIndex = _gl.GetUniformBlockIndex(handle, name);

                    int blockSize = 0;
                    _gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockDataSize, &blockSize);

                    int bindingPoint = -1;
                    _gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockBinding, &bindingPoint);

                    int activeUniforms = 0;
                    _gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockActiveUniforms, &activeUniforms);

                    int[] uniformIndices = new int[activeUniforms];
                    _gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockActiveUniformIndices, uniformIndices);

                    List<UniformMemberData> members = new List<UniformMemberData>();

                    for (int j = 0; j < uniformIndices.Length; j++)
                    {
                        int uniformIndex = uniformIndices[j];

                        byte[] uniformNameBuffer = new byte[256];
                        uint uniformNameLength = 0;
                        fixed (byte* uniformNamePtr = uniformNameBuffer)
                        {
                            _gl.GetActiveUniformName(handle, (uint)uniformIndex, (uint)uniformNameBuffer.Length, &uniformNameLength, uniformNamePtr);
                            string uniformName = Encoding.ASCII.GetString(uniformNameBuffer, 0, (int)uniformNameLength);

                            int[] offsets = new int[1];
                            fixed (int* offsetsPtr = offsets)
                            {
                                uint[] indices = new uint[] { (uint)uniformIndex };
                                fixed (uint* indicesPtr = indices)
                                {
                                    _gl.GetActiveUniforms(handle, 1, indicesPtr, GLEnum.UniformOffset, offsetsPtr);
                                }
                            }
                            int offset = offsets[0];

                            _gl.GetActiveUniform(handle, (uint)uniformIndex, out int size, out UniformType type);

                            int[] arrayStrides = new int[1];
                            fixed (int* arrayStridesPtr = arrayStrides)
                            {
                                uint[] indices = new uint[] { (uint)uniformIndex };
                                fixed (uint* indicesPtr = indices)
                                {
                                    _gl.GetActiveUniforms(handle, 1, indicesPtr, GLEnum.UniformArrayStride, arrayStridesPtr);
                                }
                            }
                            int arrayStride = arrayStrides[0];

                            int[] matrixStrides = new int[1];
                            fixed (int* matrixStridesPtr = matrixStrides)
                            {
                                uint[] indices = new uint[] { (uint)uniformIndex };
                                fixed (uint* indicesPtr = indices)
                                {
                                    _gl.GetActiveUniforms(handle, 1, indicesPtr, GLEnum.UniformMatrixStride, matrixStridesPtr);
                                }
                            }
                            int matrixStride = matrixStrides[0];

                            members.Add(new UniformMemberData(
                                uniformName,
                                (uint)uniformIndex,
                                offset,
                                size,
                                type,
                                arrayStride,
                                matrixStride
                            ));
                        }
                    }

                     var blockData = new UniformBlockData(
                        name,
                        blockIndex,
                        blockSize,
                        activeUniforms,
                        members,
                        bindingPoint > -1 ? bindingPoint : null
                    );

                    _uniformBlocks.Add(blockData);
                }
            }
        
        }

        internal unsafe static void CacheSamplerUniforms(
            GL _gl,
            uint handle,
            Dictionary<string, UniformSamplerInfo> _samplerUniforms,
            Dictionary<string, int> _uniformLocations,
            string vertexSource,
            string fragmentSource
            )
        {
            _gl.GetProgram(handle, GLEnum.ActiveUniforms, out int uniformCount);

            if (uniformCount == 0) return;

            Dictionary<string, int> bindingPoints = new Dictionary<string, int>();

            ParseShaderBindingPoints(vertexSource, bindingPoints);
            ParseShaderBindingPoints(fragmentSource, bindingPoints);

            for (uint i = 0; i < uniformCount; i++)
            {
                string uniformName = _gl.GetActiveUniform(handle, i, out int size, out UniformType type);
                if (!GlslParser.IsSamplerType(type)) continue;

                int location = _gl.GetUniformLocation(handle, uniformName);
                int? bindingPoint = null;
                if (bindingPoints.TryGetValue(uniformName, out int binding))
                {
                    bindingPoint = binding;
                }

                _uniformLocations[uniformName] = location;
                _samplerUniforms[uniformName] = new UniformSamplerInfo
                {
                    Location = location,
                    Size = size,
                    Type = type,
                    Name = uniformName,
                    BindingPoint = bindingPoint,
                };
            }
        }

        internal static void CacheSamplerUniforms(
            GL _gl, 
            uint handle,
            Dictionary<string, UniformInfo> _samplerUniforms,
            Dictionary<string, int> _uniformLocations
            )
        {
            _gl.GetProgram(handle, GLEnum.ActiveUniforms, out int uniformCount);

            for (uint i = 0; i < uniformCount; i++)
            {
                string uniformName = _gl.GetActiveUniform(handle, i, out int size, out UniformType type);
                int location = _gl.GetUniformLocation(handle, uniformName);

                if (!GlslParser.IsSamplerType(type)) continue;

                _uniformLocations[uniformName] = location;
                _samplerUniforms[uniformName] = new UniformInfo
                {
                    Location = location,
                    Size = size,
                    Type = type,
                    Name = uniformName
                };
            }
        }

        public override void Dispose()
        {
            //autoUBOHub?.Dispose();
            _gl.DeleteProgram(handle);
        }

        
        public static explicit operator uint(Shader shader) => shader.handle;
        
        public void ReserveTextureUnit(string uniformName, int textureUnit) => 
            _shaderTextureManager.ReserveTextureUnit(uniformName, textureUnit);
        public void ReserveTextureUnit(int location, int textureUnit) => 
            _shaderTextureManager.ReserveTextureUnit(location, textureUnit);


        private static void ParseShaderBindingPoints(string shaderSource, Dictionary<string, int> bindingPoints)
        {
            if (string.IsNullOrEmpty(shaderSource))
                return;
            var regex = new System.Text.RegularExpressions.Regex(
                @"layout\s*\(\s*(?:.*?binding\s*=\s*(\d+).*?|binding\s*=\s*(\d+).*?)\s*\)\s*uniform\s+(\w+)\s+(\w+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);

            var matches = regex.Matches(shaderSource);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string bindingStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                string samplerName = match.Groups[4].Value;

                if (int.TryParse(bindingStr, out int bindingPoint))
                {
                    bindingPoints[samplerName] = bindingPoint;
                }
            }
        }
        protected class ShaderTextureManager
        {
            private Shader _shader;
            private readonly Dictionary<string, int> _textureUnitMap = new Dictionary<string, int>();
            private readonly HashSet<int> _reservedUnits = new HashSet<int>();
            private int _nextTextureUnit = 0;
            private const int MaxTextureUnits = 32;

            public ShaderTextureManager(Shader shader)
            {
                _shader = shader;
            }

            public void ReserveTextureUnit(string uniformName, int textureUnit)
            {
                if (textureUnit < 0 || textureUnit >= MaxTextureUnits)
                {
                    throw new ArgumentOutOfRangeException(nameof(textureUnit),
                        $"Texture unit must be between 0 and {MaxTextureUnits - 1}");
                }

                if (_reservedUnits.Contains(textureUnit) && !_textureUnitMap.ContainsKey(uniformName))
                {
#if DEBUG
                    DebLogger.Warn($"Texture unit {textureUnit} is already reserved by another uniform");
#endif
                }

                _textureUnitMap[uniformName] = textureUnit;
                _reservedUnits.Add(textureUnit);
            }
            public void ReserveTextureUnit(int location, int textureUnit)
            {
                if (location == -1)
                {
                    return;
                }

                string uniformName = null;
                foreach (var pair in _shader._uniformLocations)
                {
                    if (pair.Value == location)
                    {
                        uniformName = pair.Key;
                        break;
                    }
                }

                if (uniformName != null)
                {
                    ReserveTextureUnit(uniformName, textureUnit);
                }
                else
                {
#if DEBUG
                    DebLogger.Warn($"Could not find uniform name for location {location}");
#endif
                }
            }

            public TextureUnit GetTextureUnitForUniform(string uniformName)
            {
                if (!_textureUnitMap.TryGetValue(uniformName, out int unitIndex))
                {
                    while (_nextTextureUnit < MaxTextureUnits && _reservedUnits.Contains(_nextTextureUnit))
                    {
                        _nextTextureUnit++;
                    }

                    if (_nextTextureUnit >= MaxTextureUnits)
                        throw new InvalidOperationException("Превышено максимальное количество текстурных блоков");

                    unitIndex = _nextTextureUnit++;
                    _textureUnitMap[uniformName] = unitIndex;
                    _reservedUnits.Add(unitIndex);
                }

                return TextureUnit.Texture0 + unitIndex;
            }

            public void BindTexture(string uniformName, Texture texture)
            {
                TextureUnit unit = GetTextureUnitForUniform(uniformName);
                if (!_shader._uniformLocations.TryGetValue(uniformName, out int location))
                {
#if DEBUG
                    DebLogger.Error($"Not exist {uniformName} into {_shader}");
#endif
                    return;
                }

                _shader._gl.ActiveTexture(unit);
                texture.Bind();
                _shader._gl.Uniform1(location, unit - TextureUnit.Texture0);
            }

            public void Reset()
            {
                _textureUnitMap.Clear();
                _reservedUnits.Clear();
                _nextTextureUnit = 0;
            }

        }
    }

    public struct UniformBlockData
    {
        public readonly string Name;
        public readonly uint BlockIndex;
        public readonly int BlockSize;
        public readonly int ActiveUniforms;
        public readonly List<UniformMemberData> Members;
        public readonly int? BindingPoint;

        public UniformBlockData(
            string name,
            uint blockIndex,
            int blockSize,
            int activeUniforms,
            List<UniformMemberData> members,
            int? bindingPoint = null)
        {
            Name = name;
            BlockIndex = blockIndex;
            BlockSize = blockSize;
            ActiveUniforms = activeUniforms;
            Members = members;
            BindingPoint = bindingPoint;
        }

        public override string ToString()
        {
            string res = string.Empty;
            foreach(var mem in Members)
            {
                res += mem.ToString();
            }
            return $"{Name} BlockIndex:{BlockIndex} Size:{BlockSize} ActiveUniforms:{ActiveUniforms}\n{res}";
        }
    }

    public struct UniformMemberData
    {
        public readonly string Name;
        public readonly uint Index;
        public readonly int Offset;
        public readonly int Size;
        public readonly UniformType Type;
        public readonly int ArrayStride;
        public readonly int MatrixStride;

        public UniformMemberData(
            string name,
            uint index,
            int offset,
            int size,
            UniformType type,
            int arrayStride,
            int matrixStride)
        {
            Name = name;
            Index = index;
            Offset = offset;
            Size = size;
            Type = type;
            ArrayStride = arrayStride;
            MatrixStride = matrixStride;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
