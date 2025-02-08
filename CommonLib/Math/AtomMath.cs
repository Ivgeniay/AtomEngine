using System.Numerics;

namespace AtomEngine
{
    public static class AtomMath
    {
        public static float DegreesToRadians(float degrees)
        {
            return MathF.PI / 180f * degrees;
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        { 
            float roll = MathF.Atan2(2.0f * (q.W * q.X + q.Y * q.Z), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y)); 
            float sinp = 2.0f * (q.W * q.Y - q.Z * q.X);
            float pitch = MathF.Abs(sinp) >= 1 ?
                MathF.CopySign(MathF.PI / 2, sinp) : 
                MathF.Asin(sinp); 
            float yaw = MathF.Atan2(2.0f * (q.W * q.Z + q.X * q.Y), 1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z));
            return new Vector3(roll, pitch, yaw);
        }

        public static Vector3 MultiplyQuaternionVectorV(Quaternion rotation, Vector3 point)
        {
            float x = rotation.X * 2f;
            float y = rotation.Y * 2f;
            float z = rotation.Z * 2f;
            float xx = rotation.X * x;
            float yy = rotation.Y * y;
            float zz = rotation.Z * z;
            float xy = rotation.X * y;
            float xz = rotation.X * z;
            float yz = rotation.Y * z;
            float wx = rotation.W * x;
            float wy = rotation.W * y;
            float wz = rotation.W * z;

            Vector3 result;
            result.X = (1f - (yy + zz)) * point.X + (xy - wz) * point.Y + (xz + wy) * point.Z;
            result.Y = (xy + wz) * point.X + (1f - (xx + zz)) * point.Y + (yz - wx) * point.Z;
            result.Z = (xz - wy) * point.X + (yz + wx) * point.Y + (1f - (xx + yy)) * point.Z;

            return result;
        }

        public static Quaternion MultiplyQuaternionVectorQ(Quaternion q, Vector3 v)
        {
            return new Quaternion(
                q.W * v.X + q.Y * v.Z - q.Z * v.Y,
                q.W * v.Y + q.Z * v.X - q.X * v.Z,
                q.W * v.Z + q.X * v.Y - q.Y * v.X,
                -q.X * v.X - q.Y * v.Y - q.Z * v.Z
            );
        }
    
    }
}
