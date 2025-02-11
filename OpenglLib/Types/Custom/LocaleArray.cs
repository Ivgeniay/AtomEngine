using AtomEngine;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Collections;

namespace OpenglLib
{
    public class LocaleArray<T> : IEnumerable<T> where T : struct
    {
        public int Location = -1;
        private T[] array;
        private GL _gl;
        private Type type;
            
        public LocaleArray(int size, GL gL)
        {
            array = new T[size];
            _gl = gL;
            type = typeof(T);
        }


        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    DebLogger.Error("Index out of Range");
                    return default;
                }
                return array[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    DebLogger.Error("Index out of Range");
                    return;
                }
                if (Location == -1)
                {
                    DebLogger.Warn("You try to set value to -1 lcation field");
                    return;
                }

                array[index] = value;
                SetUniform(Location + index, value);
            }
        }

        public int Count => array.Length;
        public T[] ToArray() => (T[])array.Clone();
        public bool Contains(T item) => Array.Exists(array, element => EqualityComparer<T>.Default.Equals(element, item));

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return array[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void SetUniform(int location, object value)
        {
            Type type = value.GetType();

            switch (type)
            {
                case Type t when t == typeof(float):
                    _gl.Uniform1(location, (float)value);
                    break;
                case Type t when t == typeof(int):
                    _gl.Uniform1(location, (int)value);
                    break;
                case Type t when t == typeof(bool):
                    _gl.Uniform1(location, (bool)value ? 1 : 0);
                    break;
                case Type t when t == typeof(double):
                    _gl.Uniform1(location, (double)value);
                    break;

                case Type t when t == typeof(Vector2D<float>):
                    var vec2 = (Vector2D<float>)value;
                    _gl.Uniform2(location, vec2.X, vec2.Y);
                    break;
                case Type t when t == typeof(Vector3D<float>):
                    var vec3 = (Vector3D<float>)value;
                    _gl.Uniform3(location, vec3.X, vec3.Y, vec3.Z);
                    break;
                case Type t when t == typeof(Vector4D<float>):
                    var vec4 = (Vector4D<float>)value;
                    _gl.Uniform4(location, vec4.X, vec4.Y, vec4.Z, vec4.W);
                    break;

                case Type t when t == typeof(Vector2D<int>):
                    var ivec2 = (Vector2D<int>)value;
                    _gl.Uniform2(location, ivec2.X, ivec2.Y);
                    break;
                case Type t when t == typeof(Vector3D<int>):
                    var ivec3 = (Vector3D<int>)value;
                    _gl.Uniform3(location, ivec3.X, ivec3.Y, ivec3.Z);
                    break;
                case Type t when t == typeof(Vector4D<int>):
                    var ivec4 = (Vector4D<int>)value;
                    _gl.Uniform4(location, ivec4.X, ivec4.Y, ivec4.Z, ivec4.W);
                    break;

                case Type t when t == typeof(Vector2D<double>):
                    var dvec2 = (Vector2D<double>)value;
                    _gl.Uniform2(location, dvec2.X, dvec2.Y);
                    break;
                case Type t when t == typeof(Vector3D<double>):
                    var dvec3 = (Vector3D<double>)value;
                    _gl.Uniform3(location, dvec3.X, dvec3.Y, dvec3.Z);
                    break;
                case Type t when t == typeof(Vector4D<double>):
                    var dvec4 = (Vector4D<double>)value;
                    _gl.Uniform4(location, dvec4.X, dvec4.Y, dvec4.Z, dvec4.W);
                    break;

                case Type t when t == typeof(Matrix2X2<float>):
                    {
                        var matrix = (Matrix2X2<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12,
                            matrix.M21, matrix.M22
                        });
                        _gl.UniformMatrix2(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix3X3<float>):
                    {
                        var matrix = (Matrix3X3<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12, matrix.M13,
                            matrix.M21, matrix.M22, matrix.M23,
                            matrix.M31, matrix.M32, matrix.M33
                        });
                        _gl.UniformMatrix3(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix4X4<float>):
                    {
                        var matrix = (Matrix4X4<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                            matrix.M41, matrix.M42, matrix.M43, matrix.M44
                        });
                        _gl.UniformMatrix4(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix2X3<float>):
                    {
                        var matrix = (Matrix2X3<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12, matrix.M13,
                            matrix.M21, matrix.M22, matrix.M23
                        });
                        _gl.UniformMatrix2x3(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix2X4<float>):
                    {
                        var matrix = (Matrix2X4<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                            matrix.M21, matrix.M22, matrix.M23, matrix.M24
                        });
                        _gl.UniformMatrix2x4(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix3X2<float>):
                    {
                        var matrix = (Matrix3X2<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12,
                            matrix.M21, matrix.M22,
                            matrix.M31, matrix.M32
                        });
                        _gl.UniformMatrix3x2(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix3X4<float>):
                    {
                        var matrix = (Matrix3X4<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                            matrix.M31, matrix.M32, matrix.M33, matrix.M34
                        });
                        _gl.UniformMatrix3x4(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix4X2<float>):
                    {
                        var matrix = (Matrix4X2<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12,
                            matrix.M21, matrix.M22,
                            matrix.M31, matrix.M32,
                            matrix.M41, matrix.M42
                        });
                        _gl.UniformMatrix4x2(location, 1, false, span);
                    }
                    break;
                case Type t when t == typeof(Matrix4X3<float>):
                    {
                        var matrix = (Matrix4X3<float>)value;
                        var span = new ReadOnlySpan<float>(new[] {
                            matrix.M11, matrix.M12, matrix.M13,
                            matrix.M21, matrix.M22, matrix.M23,
                            matrix.M31, matrix.M32, matrix.M33,
                            matrix.M41, matrix.M42, matrix.M43
                        });
                        _gl.UniformMatrix4x3(location, 1, false, span);
                    }
                    break;

                case Type t when t == typeof(int) && value is int sampler:
                    _gl.Uniform1(location, sampler);
                    break;

                default:
                    throw new ArgumentException($"Unsupported uniform type: {type}");
            }
        }

    }
}
