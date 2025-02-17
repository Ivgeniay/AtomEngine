using System.Numerics;

namespace AtomEngine
{
    public static class AtomMath
    {
        public static float DegreesToRadians(this float degrees) => MathF.PI / 180f * degrees;
        public static float RadiansToDegrees(this float radians) => 180f / MathF.PI * radians;

        public static Quaternion ToQuaternion(this Vector3 eulerAngles) => (eulerAngles * (MathF.PI / 180f)).ToQuaternionFromRad();

        public static Quaternion ToQuaternionFromRad(this Vector3 eulerAngles)
        {
            float pitch = eulerAngles.X;
            float yaw = eulerAngles.Y;
            float roll = eulerAngles.Z;

            float cy = MathF.Cos(yaw * 0.5f);
            float sy = MathF.Sin(yaw * 0.5f);
            float cp = MathF.Cos(pitch * 0.5f);
            float sp = MathF.Sin(pitch * 0.5f);
            float cr = MathF.Cos(roll * 0.5f);
            float sr = MathF.Sin(roll * 0.5f);

            return new Quaternion(
                sr * cp * cy - cr * sp * sy, 
                cr * sp * cy + sr * cp * sy, 
                cr * cp * sy - sr * sp * cy, 
                cr * cp * cy + sr * sp * sy  
            );
        }

        public static Vector3 ToEuler(this Quaternion q) => ToEulerRad(q) * (180f / MathF.PI);
        public static Vector3 ToEulerRad(this Quaternion q)
        {
            float sinr_cosp = 2.0f * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y);
            float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2.0f * (q.W * q.Y - q.Z * q.X);
            float pitch = MathF.Abs(sinp) >= 1.0f
                ? MathF.CopySign(MathF.PI / 2, sinp)
                : MathF.Asin(sinp);

            float siny_cosp = 2.0f * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z);
            float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

            return new Vector3(pitch, yaw, roll); 
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
