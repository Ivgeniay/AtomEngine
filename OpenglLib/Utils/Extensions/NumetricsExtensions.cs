using Silk.NET.Maths;
using System.Numerics;

namespace OpenglLib
{
    public static class NumetricsExtensions
    {


        public static Vector2 ToNumetrix(this Vector2D<int> vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Vector2 ToNumetrix(this Vector2D<float> vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Vector2D<float> ToSilk(this Vector2 vector)
        {
            return new Vector2D<float>(vector.X, vector.Y);
        }




        public static Vector3 ToNumetrix(this Vector3D<float> vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }
        public static Vector3D<float> ToSilk(this Vector3 vector)
        {
            return new Vector3D<float>(vector.X, vector.Y, vector.Z);
        }

        public static Vector4 ToNumetrix(this Vector4D<float> vector)
        {
            return new Vector4(vector.X, vector.Y, vector.Z, vector.W);
        }
        public static Vector4D<float> ToSilk(this Vector4 vector)
        {
            return new Vector4D<float>(vector.X, vector.Y, vector.Z, vector.W);
        }


        public static Matrix4X4<float> ToSilk(this Matrix4x4 matrix)
        {
            return new Matrix4X4<float>(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }
        public static Matrix4x4 ToNumetrix(this Matrix4X4<float> matrix)
        {
            return new Matrix4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }
    }
}
