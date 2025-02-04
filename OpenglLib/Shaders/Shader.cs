using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class Shader : IDisposable
    {
        private readonly uint _handle;
        private readonly GL _gl;
        private readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();
        private readonly Dictionary<string, uint> _attributeLocations = new Dictionary<string, uint>();
        private readonly Dictionary<string, UniformInfo> _uniformInfo = new Dictionary<string, UniformInfo>();

        public uint Handle => _handle;

        string vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
}";

        string fragmentShaderSource = @"
#version 330 core
out vec4 FragColor;

uniform vec3 color;

void main()
{
    FragColor = vec4(color, 1.0);
}";

        public Shader(GL gl, string vertexSource = "", string fragmentSource = "")
        {
            _gl = gl;

            vertexSource = string.IsNullOrEmpty(vertexSource) ? vertexShaderSource : vertexSource;
            fragmentSource = string.IsNullOrEmpty(fragmentSource) ? fragmentShaderSource : fragmentSource;

            Result<uint, Error> mb_vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint vertexShader = mb_vertexShader.Unwrap();

            Result<uint, Error> mb_fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
            uint fragmentShader = mb_fragmentShader.Unwrap();

            // Создаем и линкуем программу
            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);
            _gl.AttachShader(_handle, fragmentShader);
            LinkProgram();

            // Очищаем ресурсы
            _gl.DetachShader(_handle, vertexShader);
            _gl.DetachShader(_handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            CacheAttributes();
            CacheUniforms();
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
            _gl.LinkProgram(_handle);
            _gl.GetProgram(_handle, GLEnum.LinkStatus, out int success);
            if (success == 0)
            {
                throw new Exception($"Error linking shader program: {_gl.GetProgramInfoLog(_handle)}");
            }
        }

        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        public void SetUniform(string name, object value)
        {
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
                throw new ArgumentError($"Uniform {name} not found in shader program");
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

                // Векторы (float)
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

                // Векторы (int)
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

                // Матрицы
                case UniformType.FloatMat2:
                    unsafe
                    {
                        var mat2 = (Matrix2X2<float>)value;
                        _gl.UniformMatrix2(location, 1, false, (float*)&mat2);
                    }
                    break;
                case UniformType.FloatMat3:
                    unsafe
                    {
                        var mat3 = (Matrix3X3<float>)value;
                        _gl.UniformMatrix3(location, 1, false, (float*)&mat3);
                    }
                    break;
                case UniformType.FloatMat4:
                    unsafe
                    {
                        var mat4 = (Matrix4X4<float>)value;
                        _gl.UniformMatrix4(location, 1, false, (float*)&mat4);
                    }
                    break;
                case UniformType.FloatMat2x3:
                    unsafe
                    {
                        var mat2x3 = (Matrix2X3<float>)value;
                        _gl.UniformMatrix2x3(location, 1, false, (float*)&mat2x3);
                    }
                    break;
                case UniformType.FloatMat2x4:
                    unsafe
                    {
                        var mat2x4 = (Matrix2X4<float>)value;
                        _gl.UniformMatrix2x4(location, 1, false, (float*)&mat2x4);
                    }
                    break;
                case UniformType.FloatMat3x2:
                    unsafe
                    {
                        var mat3x2 = (Matrix3X2<float>)value;
                        _gl.UniformMatrix3x2(location, 1, false, (float*)&mat3x2);
                    }
                    break;
                case UniformType.FloatMat3x4:
                    unsafe
                    {
                        var mat3x4 = (Matrix3X4<float>)value;
                        _gl.UniformMatrix3x4(location, 1, false, (float*)&mat3x4);
                    }
                    break;
                case UniformType.FloatMat4x2:
                    unsafe
                    {
                        var mat4x2 = (Matrix4X2<float>)value;
                        _gl.UniformMatrix4x2(location, 1, false, (float*)&mat4x2);
                    }
                    break;
                case UniformType.FloatMat4x3:
                    unsafe
                    {
                        var mat4x3 = (Matrix4X3<float>)value;
                        _gl.UniformMatrix4x3(location, 1, false, (float*)&mat4x3);
                    }
                    break;

                // Сэмплеры
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

        internal IReadOnlyDictionary<string, UniformInfo> GetAllUniformInfo()
        {
            return _uniformInfo;
        }

        internal IReadOnlyDictionary<string, uint> GetAllAttributeLocations()
        {
            return _attributeLocations;
        }

        private void CacheAttributes()
        {
            _gl.GetProgram(_handle, GLEnum.ActiveAttributes, out int attributeCount);

            for (uint i = 0; i < attributeCount; i++)
            {
                string attributeName = _gl.GetActiveAttrib(_handle, i, out int size, out AttributeType type);
                uint location = (uint)_gl.GetAttribLocation(_handle, attributeName);
                _attributeLocations[attributeName] = location;
            }
        }

        private void CacheUniforms()
        {
            _gl.GetProgram(_handle, GLEnum.ActiveUniforms, out int uniformCount);

            for (int i = 0; i < uniformCount; i++)
            {
                string uniformName = _gl.GetActiveUniform(_handle, (uint)i, out int size, out UniformType type);
                int location = _gl.GetUniformLocation(_handle, uniformName);

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

        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}
