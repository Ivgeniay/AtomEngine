﻿using AtomEngine;
using EngineLib;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    public class AutoUBO : IDisposable
    {
        private readonly Dictionary<string, UniformMemberData> _members = new Dictionary<string, UniformMemberData>();
        private uint    _handle;
        private GL      _gl;
        private uint?   _bindingPoint;
        private string  _blockName;
        private int     _blockSize;
        private byte[]  _buffer;

        public uint Handle { get => _handle; }
        public uint? BindingPoint { get => _bindingPoint; }
        public string BlockName { get => _blockName; }
        public int BlockSize { get => _blockSize; }
        public bool IsDirty { get; set; } = false;
        public Dictionary<string, object> _values = new Dictionary<string, object>(); 

        public AutoUBO(GL gl, UniformBlockData blockData)
        {
            _gl = gl;
            _blockName = blockData.Name;
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

            _bindingPoint = (uint)blockData.BindingPoint.Value;

            unsafe
            {
                _gl.BindBufferBase(BufferTargetARB.UniformBuffer, _bindingPoint.Value, _handle);
            }
        }

        public AutoUBO(GL gl, string blockName, uint bindingPoint, int blockSize)
        {
            _gl = gl;
            _blockName = blockName;
            _blockSize = blockSize;

            _buffer = new byte[_blockSize];

            _handle = _gl.GenBuffer();
            Bind();

            unsafe
            {
                _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)_blockSize, null, BufferUsageARB.DynamicDraw);
            }

            var bindingService = ServiceHub.Get<BindingPointService>();
            try
            {
                bindingService.AllocateGlobalBindingPoint(bindingPoint);
                _bindingPoint = bindingPoint;
            }
            catch (InvalidOperationError ex)
            {
                throw new InvalidOperationError($"Не удалось зарезервировать точку привязки {bindingPoint} для блока {blockName}: {ex.Message}");
            }

            unsafe
            {
                _gl.BindBufferBase(BufferTargetARB.UniformBuffer, _bindingPoint.Value, _handle);
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
                    _values[name] = value;
                }
                IsDirty = true;
            }
            else
            {
                DebLogger.Error($"Member {name} not found in uniform block {_blockName}");
            }
        }

        public void SetUniforms(Dictionary<string, object> values)
        {
            foreach (var pair in values)
            {
                SetUniform(pair.Key, pair.Value);
            }
        }

        public void SetRawByte(int offset, byte value)
        {
            if (offset < 0 || offset >= _buffer.Length)
            {
                DebLogger.Error($"Offset {offset} is out of range [0, {_buffer.Length})");
                return;
            }

            _buffer[offset] = value;
            IsDirty = true;
        }

        public bool HasUniform(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return _members.ContainsKey(name);
        }

        internal uint GetBindingPoint()
        {
            if (!_bindingPoint.HasValue)
            {
                throw new InvalidOperationError("Binding point is not set");
            }
            return _bindingPoint.Value;
        }

        public string GetBlockName()
        {
            return _blockName;
        }

        public int GetBlockSize()
        {
            return _blockSize;
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

                // Прочие case для других типов данных...

                default:
                    DebLogger.Error($"Unsupported uniform type at offset {offset}: {type}");
                    break;
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
            if (IsDirty)
            {
                Bind();
                unsafe
                {
                    fixed (byte* bufferPtr = _buffer)
                    {
                        _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)_buffer.Length, bufferPtr);
                    }
                }
                IsDirty = false;
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
                bindingService.ReleaseGlobalBindingPoint(_bindingPoint.Value);
            }
        }

        public bool TryGetValue(string uniformName, out object value)
        {
            return _values.TryGetValue(uniformName, out value);
        }
    }

}
