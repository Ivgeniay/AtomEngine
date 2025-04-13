using AtomEngine;
using EngineLib;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    public class AutoUBO : IDisposable
    {
        private uint _handle;
        private GL _gl;
        private uint _program;
        private uint? _bindingPoint;
        private string _blockName;
        private uint _blockIndex;
        private int _blockSize;
        private readonly Dictionary<string, UniformMemberData> _members = new Dictionary<string, UniformMemberData>();
        private byte[] _buffer;
        private bool _isDirty = false;

        public AutoUBO(GL gl, uint program, UniformBlockData blockData)
        {
            _gl = gl;
            _program = program;
            _blockName = blockData.Name;
            _blockIndex = blockData.BlockIndex;
            _blockSize = blockData.BlockSize;

            _buffer = new byte[_blockSize];
            foreach (var member in blockData.Members)
            {
                _members[member.Name] = member;
            }

            _handle = _gl.GenBuffer();
            Bind();

            unsafe
            {
                _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)_blockSize, null, BufferUsageARB.DynamicDraw);
            }

            if (blockData.BindingPoint > -1) _bindingPoint = (uint)blockData.BindingPoint;
            else
            {
                var bindingService = ServiceHub.Get<BindingPointService>();
                _bindingPoint = bindingService.AllocateBindingPoint(_program);
            }

            if (_bindingPoint.HasValue)
            {
                unsafe
                {
                    _gl.UniformBlockBinding(_program, _blockIndex, _bindingPoint.Value);
                    _gl.BindBufferBase(BufferTargetARB.UniformBuffer, _bindingPoint.Value, _handle);
                }
            }
        }

        public void SetUniform(string name, object value)
        {
            if (value == null)
            {
                DebLogger.Error($"Cannot set null value for {name}");
                return;
            }

            if (_members.TryGetValue(name, out var member))
            {
                unsafe
                {
                    WriteValueToBuffer(member.Offset, member.Type, value, member.MatrixStride);
                }
                _isDirty = true;
            }
            else
            {
                DebLogger.Error($"Member {name} not found in uniform block {_blockName}");
            }
        }

        public bool HasUniform(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return _members.ContainsKey(name);
        }

        private unsafe void WriteValueToBuffer(int offset, UniformType type, object value, int matrixStride)
        {
            switch (type)
            {
                case UniformType.Float:
                    if (value is float floatValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(float*)bufferPtr = floatValue;
                        }
                    }
                    else if (value is double _doubleValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(float*)bufferPtr = (float)_doubleValue;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected float, got {value.GetType()}");
                    }
                    break;

                case UniformType.Int:
                    if (value is int intValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(int*)bufferPtr = intValue;
                        }
                    }
                    else if (value is uint _uintValue && _uintValue <= int.MaxValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(int*)bufferPtr = (int)_uintValue;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected int, got {value.GetType()}");
                    }
                    break;

                case UniformType.UnsignedInt:
                    if (value is uint uintValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(uint*)bufferPtr = uintValue;
                        }
                    }
                    else if (value is int _intValue && _intValue >= 0)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(uint*)bufferPtr = (uint)_intValue;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected uint, got {value.GetType()}");
                    }
                    break;

                case UniformType.Double:
                    if (value is double doubleValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(double*)bufferPtr = doubleValue;
                        }
                    }
                    else if (value is float _floatValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(double*)bufferPtr = _floatValue;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected double, got {value.GetType()}");
                    }
                    break;

                case UniformType.Bool:
                    if (value is bool boolValue)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(int*)bufferPtr = boolValue ? 1 : 0;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected bool, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatVec2:
                    if (value is Vector2D<float> vec2)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector2D<float>*)bufferPtr = vec2;
                        }
                    }
                    else if (value is System.Numerics.Vector2 vec2Sys)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector2D<float>*)bufferPtr = new Vector2D<float>(vec2Sys.X, vec2Sys.Y);
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector2D<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatVec3:
                    if (value is Vector3D<float> vec3)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector3D<float>*)bufferPtr = vec3;
                        }
                    }
                    else if (value is System.Numerics.Vector3 vec3Sys)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector3D<float>*)bufferPtr = new Vector3D<float>(vec3Sys.X, vec3Sys.Y, vec3Sys.Z);
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector3D<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatVec4:
                    if (value is Vector4D<float> vec4)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector4D<float>*)bufferPtr = vec4;
                        }
                    }
                    else if (value is System.Numerics.Vector4 vec4Sys)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector4D<float>*)bufferPtr = new Vector4D<float>(vec4Sys.X, vec4Sys.Y, vec4Sys.Z, vec4Sys.W);
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector4D<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.IntVec2:
                    if (value is Vector2D<int> ivec2)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector2D<int>*)bufferPtr = ivec2;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector2D<int>, got {value.GetType()}");
                    }
                    break;

                case UniformType.IntVec3:
                    if (value is Vector3D<int> ivec3)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector3D<int>*)bufferPtr = ivec3;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector3D<int>, got {value.GetType()}");
                    }
                    break;

                case UniformType.IntVec4:
                    if (value is Vector4D<int> ivec4)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector4D<int>*)bufferPtr = ivec4;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector4D<int>, got {value.GetType()}");
                    }
                    break;

                case UniformType.UnsignedIntVec2:
                    if (value is Vector2D<uint> uvec2)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector2D<uint>*)bufferPtr = uvec2;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector2D<uint>, got {value.GetType()}");
                    }
                    break;

                case UniformType.UnsignedIntVec3:
                    if (value is Vector3D<uint> uvec3)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector3D<uint>*)bufferPtr = uvec3;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector3D<uint>, got {value.GetType()}");
                    }
                    break;

                case UniformType.UnsignedIntVec4:
                    if (value is Vector4D<uint> uvec4)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector4D<uint>*)bufferPtr = uvec4;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector4D<uint>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat2:
                    if (value is Matrix2X2<float> mat2)
                    {
                        WriteMatrix2(offset, mat2, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix2X2<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat3:
                    if (value is Matrix3X3<float> mat3)
                    {
                        WriteMatrix3(offset, mat3, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix3X3<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat2x3:
                    if (value is Matrix2X3<float> mat2x3)
                    {
                        WriteMatrix2x3(offset, mat2x3, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix2X3<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat2x4:
                    if (value is Matrix2X4<float> mat2x4)
                    {
                        WriteMatrix2x4(offset, mat2x4, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix2X4<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat3x2:
                    if (value is Matrix3X2<float> mat3x2)
                    {
                        WriteMatrix3x2(offset, mat3x2, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix3X2<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat3x4:
                    if (value is Matrix3X4<float> mat3x4)
                    {
                        WriteMatrix3x4(offset, mat3x4, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix3X4<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat4x2:
                    if (value is Matrix4X2<float> mat4x2)
                    {
                        WriteMatrix4x2(offset, mat4x2, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix4X2<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat4x3:
                    if (value is Matrix4X3<float> mat4x3)
                    {
                        WriteMatrix4x3(offset, mat4x3, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix4X3<float>, got {value.GetType()}");
                    }
                    break;

                case UniformType.DoubleMat2:
                    if (value is Matrix2X2<double> dmat2)
                    {
                        WriteDoubleMatrix2(offset, dmat2, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix2X2<double>, got {value.GetType()}");
                    }
                    break;

                case UniformType.DoubleMat3:
                    if (value is Matrix3X3<double> dmat3)
                    {
                        WriteDoubleMatrix3(offset, dmat3, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix3X3<double>, got {value.GetType()}");
                    }
                    break;

                case UniformType.DoubleMat4:
                    if (value is Matrix4X4<double> dmat4)
                    {
                        WriteDoubleMatrix4(offset, dmat4, matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix4X4<double>, got {value.GetType()}");
                    }
                    break;
                
                case UniformType.DoubleVec2:
                    if (value is Vector2D<double> dvec2)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector2D<double>*)bufferPtr = dvec2;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector2D<double>, got {value.GetType()}");
                    }
                    break;

                case UniformType.DoubleVec3:
                    if (value is Vector3D<double> dvec3)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector3D<double>*)bufferPtr = dvec3;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector3D<double>, got {value.GetType()}");
                    }
                    break;

                case UniformType.DoubleVec4:
                    if (value is Vector4D<double> dvec4)
                    {
                        fixed (byte* bufferPtr = &_buffer[offset])
                        {
                            *(Vector4D<double>*)bufferPtr = dvec4;
                        }
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Vector4D<double>, got {value.GetType()}");
                    }
                    break;

                case UniformType.FloatMat4:
                    if (value is Matrix4X4<float> mat4)
                    {
                        WriteMatrix4(offset, mat4, matrixStride);
                    }
                    else if (value is System.Numerics.Matrix4x4 mat4Sys)
                    {
                        WriteMatrix4(offset, mat4Sys.ToSilk(), matrixStride);
                    }
                    else
                    {
                        DebLogger.Error($"Type mismatch at offset {offset}. Expected Matrix4X4<float>, got {value.GetType()}");
                    }
                    break;

                default:
                    DebLogger.Error($"Unsupported uniform type at offset {offset}: {type}");
                    break;
            }
        }


        private unsafe void WriteMatrix2x3(int offset, Matrix2X3<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 2; col++)
                {
                    for (int row = 0; row < 3; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix2X3<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix2x4(int offset, Matrix2X4<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 2; col++)
                {
                    for (int row = 0; row < 4; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix2X4<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix3x2(int offset, Matrix3X2<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 3; col++)
                {
                    for (int row = 0; row < 2; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix3X2<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix3x4(int offset, Matrix3X4<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 3; col++)
                {
                    for (int row = 0; row < 4; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix3X4<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix4x2(int offset, Matrix4X2<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 4; col++)
                {
                    for (int row = 0; row < 2; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix4X2<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix4x3(int offset, Matrix4X3<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 4; col++)
                {
                    for (int row = 0; row < 3; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix4X3<float>*)bufferPtr = matrix;
                }
            }
        }


        private unsafe void WriteDoubleMatrix2(int offset, Matrix2X2<double> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 2; col++)
                {
                    for (int row = 0; row < 2; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(double);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(double*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix2X2<double>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteDoubleMatrix3(int offset, Matrix3X3<double> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 3; col++)
                {
                    for (int row = 0; row < 3; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(double);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(double*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix3X3<double>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteDoubleMatrix4(int offset, Matrix4X4<double> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 4; col++)
                {
                    for (int row = 0; row < 4; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(double);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(double*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix4X4<double>*)bufferPtr = matrix;
                }
            }
        }
        
        private unsafe void WriteMatrix2(int offset, Matrix2X2<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 2; col++)
                {
                    for (int row = 0; row < 2; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix2X2<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix3(int offset, Matrix3X3<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 3; col++)
                {
                    for (int row = 0; row < 3; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix3X3<float>*)bufferPtr = matrix;
                }
            }
        }

        private unsafe void WriteMatrix4(int offset, Matrix4X4<float> matrix, int matrixStride)
        {
            if (matrixStride > 0)
            {
                for (int col = 0; col < 4; col++)
                {
                    for (int row = 0; row < 4; row++)
                    {
                        int elemOffset = offset + col * matrixStride + row * sizeof(float);
                        fixed (byte* bufferPtr = &_buffer[elemOffset])
                        {
                            *(float*)bufferPtr = matrix[col, row];
                        }
                    }
                }
            }
            else
            {
                fixed (byte* bufferPtr = &_buffer[offset])
                {
                    *(Matrix4X4<float>*)bufferPtr = matrix;
                }
            }
        }


        public void Update()
        {
            if (_isDirty)
            {
                Bind();
                unsafe
                {
                    fixed (byte* bufferPtr = _buffer)
                    {
                        _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)_buffer.Length, bufferPtr);
                    }
                }
                _isDirty = false;
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, _handle);
        }

        public void Unbind()
        {
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);

            if (_bindingPoint.HasValue)
            {
                var bindingService = ServiceHub.Get<BindingPointService>();
                bindingService.ReleaseBindingPoint(_program, _bindingPoint.Value);
            }
        }
    }

    public class AutoUBOHub : IDisposable
    {
        private readonly List<AutoUBO> _uboList = new List<AutoUBO>();
        private readonly GL _gl;
        private readonly uint _program;
        private bool _isDirty = false;

        public AutoUBOHub(GL gl, uint program)
        {
            _gl = gl;
            _program = program;
        }

        public AutoUBO RegisterUBO(UniformBlockData blockData)
        {
            var ubo = new AutoUBO(_gl, _program, blockData);
            _uboList.Add(ubo);
            return ubo;
        }

        public bool SetUniform(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (var ubo in _uboList)
            {
                if (ubo.HasUniform(name))
                {
                    ubo.SetUniform(name, value);
                    _isDirty = true;
                    return true;
                }
            }

            return false;
        }

        public void Update()
        {
            if (!_isDirty) return;

            foreach (var ubo in _uboList)
            {
                ubo.Update();
            }

            _isDirty = false;
        }

        public void Dispose()
        {
            foreach (var ubo in _uboList)
            {
                ubo.Dispose();
            }
            _uboList.Clear();
        }
    }
}
