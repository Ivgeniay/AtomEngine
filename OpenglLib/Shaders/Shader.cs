using AtomEngine;
using AtomEngine.RenderEntity;
using EngineLib;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Text;

namespace OpenglLib
{
    public class Shader : ShaderBase
    {
        public readonly GL _gl;

        protected readonly ShaderTextureManager _shaderTextureManager;

        protected readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();
        protected readonly Dictionary<string, uint> _attributeLocations = new Dictionary<string, uint>();
        protected readonly Dictionary<string, UniformInfo> _uniformInfo = new Dictionary<string, UniformInfo>();
        protected readonly List<UniformBlockData> _uniformBlocks = new List<UniformBlockData>();

        protected string VertexSource;
        protected string FragmentSource;

        public Shader(GL gl, string vertexSource = "", string fragmentSource = "")
        {
            _gl = gl;
            _shaderTextureManager = new ShaderTextureManager(this);
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

            CacheAttributes();
            CacheUniforms();
            CacheUniformBlocks();

            DebLogger.Info("==================SHADER_ATTRIBUTES=================");
            foreach (var item in _attributeLocations) DebLogger.Info(item.Key, " : ", item.Value);
            DebLogger.Info("===================SHADER_UNIFORMS==================");
            foreach (var item in _uniformLocations) DebLogger.Info(item.Key, " : ", item.Value);
            DebLogger.Info("===================UNIFORM_BLOCKS==================");
            foreach (var item in _uniformBlocks) DebLogger.Info(item);
        }

        public override void Use()
        {
            _gl.UseProgram(handle);
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

            switch (_uniformInfo[name].Type)
            {
                case UniformType.Float:
                    _gl.Uniform1(location, Convert.ToSingle(value));
                    break;
                case UniformType.Int:
                    _gl.Uniform1(location, Convert.ToInt32(value));
                    break;
                case UniformType.Bool:
                    _gl.Uniform1(location, Convert.ToBoolean(value) ? 1 : 0);
                    break;
                case UniformType.Double:
                    _gl.Uniform1(location, Convert.ToDouble(value));
                    break;

                case UniformType.FloatVec2:
                    var vec2 = (Vector2D<float>)value;
                    _gl.Uniform2(location, vec2.X, vec2.Y);
                    break;
                case UniformType.FloatVec3:
                    var vec3 = (Vector3D<float>)value;
                    _gl.Uniform3(location, vec3.X, vec3.Y, vec3.Z);
                    break;
                case UniformType.FloatVec4:
                    var vec4 = (Vector4D<float>)value;
                    _gl.Uniform4(location, vec4.X, vec4.Y, vec4.Z, vec4.W);
                    break;

                case UniformType.IntVec2:
                    var ivec2 = (Vector2D<int>)value;
                    _gl.Uniform2(location, ivec2.X, ivec2.Y);
                    break;
                case UniformType.IntVec3:
                    var ivec3 = (Vector3D<int>)value;
                    _gl.Uniform3(location, ivec3.X, ivec3.Y, ivec3.Z);
                    break;
                case UniformType.IntVec4:
                    var ivec4 = (Vector4D<int>)value;
                    _gl.Uniform4(location, ivec4.X, ivec4.Y, ivec4.Z, ivec4.W);
                    break;

                case UniformType.FloatMat2:
                    unsafe
                    {
                        if (value is Matrix2X2<float> mat2)
                        {
                            _gl.UniformMatrix2(location, 1, false, (float*)&mat2);
                        }
                    }
                    break;
                case UniformType.FloatMat3:
                    unsafe
                    {
                        if (value is Matrix3X3<float> mat3)
                        {
                            _gl.UniformMatrix3(location, 1, false, (float*)&mat3);
                        }
                    }
                    break;
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
                    break;
                case UniformType.FloatMat2x3:
                    unsafe
                    {
                        if (value is Matrix2X3<float> mat2x3)
                        {
                            _gl.UniformMatrix2x3(location, 1, false, (float*)&mat2x3);
                        }
                    }
                    break;
                case UniformType.FloatMat2x4:
                    unsafe
                    {
                        if (value is Matrix2X4<float> mat2x4)
                            _gl.UniformMatrix2x4(location, 1, false, (float*)&mat2x4);
                    }
                    break;
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
                    break;
                case UniformType.FloatMat3x4:
                    unsafe
                    {
                        if (value is Matrix3X4<float> mat3x4)
                        {
                            _gl.UniformMatrix3x4(location, 1, false, (float*)&mat3x4);
                        }
                    }
                    break;
                case UniformType.FloatMat4x2:
                    unsafe
                    {
                        if (value is Matrix4X2<float> mat4x2)
                        {
                            _gl.UniformMatrix4x2(location, 1, false, (float*)&mat4x2);
                        }
                    }
                    break;
                case UniformType.FloatMat4x3:
                    unsafe
                    {
                        if (value is Matrix4X3<float> mat4x3)
                        {
                            _gl.UniformMatrix4x3(location, 1, false, (float*)&mat4x3);
                        }
                    }
                    break;

                case UniformType.Sampler1D:
                case UniformType.Sampler2D:
                case UniformType.Sampler3D:
                case UniformType.SamplerCube:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DShadow:
                case UniformType.SamplerCubeShadow:
                    _gl.Uniform1(location, Convert.ToInt32(value));
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported uniform type. Uniform: {name}, Type: {_uniformInfo[name].Type}");
            }
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

            if (!_uniformLocations.TryGetValue(uniformName, out _))
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

        private void CacheAttributes()
        {
            _gl.GetProgram(handle, GLEnum.ActiveAttributes, out int attributeCount);

            for (uint i = 0; i < attributeCount; i++)
            {
                string attributeName = _gl.GetActiveAttrib(handle, i, out int size, out AttributeType type);
                uint location = (uint)_gl.GetAttribLocation(handle, attributeName);
                _attributeLocations[attributeName] = location;
            }
        }

        private void CacheUniforms()
        {
            _gl.GetProgram(handle, GLEnum.ActiveUniforms, out int uniformCount);

            for (int i = 0; i < uniformCount; i++)
            {
                string uniformName = _gl.GetActiveUniform(handle, (uint)i, out int size, out UniformType type);
                int location = _gl.GetUniformLocation(handle, uniformName);

                _uniformLocations[uniformName] = location;
                _uniformInfo[uniformName] = new UniformInfo
                {
                    Location = location,
                    Size = size,
                    Type = type,
                    Name = uniformName
                };
            }
        }

        private unsafe void CacheUniformBlocks()
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
                    _uniformBlocks.Add(new UniformBlockData(name, blockIndex));
                }
            }
        }

        protected uint GetBlockIndex(string name)
        {
            var uniform = _uniformBlocks.FirstOrDefault(block => block.Name == name);
            return uniform.BlockIndex;
        }

        public override void Dispose()
        {
            _gl.DeleteProgram(handle);
        }

        
        public static explicit operator uint(Shader shader) => shader.handle;
        public void ReserveTextureUnit(string uniformName, int textureUnit) => 
            _shaderTextureManager.ReserveTextureUnit(uniformName, textureUnit);
        public void ReserveTextureUnit(int location, int textureUnit) => 
            _shaderTextureManager.ReserveTextureUnit(location, textureUnit);

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
        public readonly string Name = string.Empty;
        public readonly uint BlockIndex = uint.MaxValue;

        public UniformBlockData(string name, uint blockIndex)
        {
            Name = name;
            BlockIndex = blockIndex;
        }

        public override string ToString()
        {
            return $"{Name} BlockIndex:{BlockIndex}";
        }
    }
}
