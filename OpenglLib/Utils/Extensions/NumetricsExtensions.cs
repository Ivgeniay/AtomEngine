using Silk.NET.Maths;
using System.Numerics;

namespace OpenglLib
{
    public static class NumetricsExtensions
    {
        public static Silk.NET.Maths.Matrix4X4<float> ToSilk(this System.Numerics.Matrix4x4 matrix)
        {
            return new Silk.NET.Maths.Matrix4X4<float>(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }

        // Опционально: метод для обратной конвертации
        public static System.Numerics.Matrix4x4 ToSystem(this Silk.NET.Maths.Matrix4X4<float> matrix)
        {
            return new System.Numerics.Matrix4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }
    }
}
