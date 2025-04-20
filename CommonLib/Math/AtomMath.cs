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

        public static Vector3 ToEulerFromRad(this Vector3 radAngles) => new Vector3(
            radAngles.X.RadiansToDegrees(), 
            radAngles.Y.RadiansToDegrees(), 
            radAngles.Z.RadiansToDegrees());

        public static Vector3 ToRadFromEuler(this Vector3 eulerAngles) => new Vector3(
            eulerAngles.X.DegreesToRadians(),
            eulerAngles.Y.DegreesToRadians(),
            eulerAngles.Z.DegreesToRadians());

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





        public static Matrix4x4 GetXRotationFromRad(float rotRadX)
        {
            return new Matrix4x4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, (float)Math.Cos(rotRadX), (float)-Math.Sin(rotRadX), 0.0f,
                0.0f, (float)Math.Sin(rotRadX), (float)Math.Cos(rotRadX), 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }
        public static Matrix4x4 GetYRotationFromRad(float rotRadY)
        {
            return new Matrix4x4(
                (float)Math.Cos(rotRadY), 0.0f, (float)Math.Sin(rotRadY), 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                (float)-Math.Sin(rotRadY), 0.0f, (float)Math.Cos(rotRadY), 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }
        public static Matrix4x4 GetZRotationFromRad(float rotRadZ)
        {
            return new Matrix4x4(
                (float)Math.Cos(rotRadZ), (float)-Math.Sin(rotRadZ), 0.0f, 0.0f,
                (float)Math.Sin(rotRadZ), (float)Math.Cos(rotRadZ), 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,            
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }
        public static Matrix4x4 RotateFromRad(float rotRadX, float rotRadY, float rotRadZ)
        {
            return GetZRotationFromRad(rotRadZ) * GetYRotationFromRad(rotRadY) * GetXRotationFromRad(rotRadX);
        }

        public static Matrix4x4 GetXRotationFromEuler(float eulerX) => GetXRotationFromRad(eulerX.DegreesToRadians());
        public static Matrix4x4 GetYRotationFromEuler(float eulerY) => GetYRotationFromRad(eulerY.DegreesToRadians());
        public static Matrix4x4 GetZRotationFromEuler(float eulerZ) => GetZRotationFromRad(eulerZ.DegreesToRadians());

        public static Matrix4x4 RotateFromEuler(float eulerX, float eulerY, float eulerZ, EulerOrder order = EulerOrder.ZYX)
        {
            var rx = GetXRotationFromEuler(eulerX);
            var ry = GetYRotationFromEuler(eulerY);
            var rz = GetZRotationFromEuler(eulerZ);

            return order switch
            {
                EulerOrder.XYZ => rx * ry * rz,
                EulerOrder.XZY => rx * rz * ry,
                EulerOrder.YXZ => ry * rx * rz,
                EulerOrder.YZX => ry * rz * rx,
                EulerOrder.ZXY => rz * rx * ry,
                EulerOrder.ZYX => rz * ry * rx,
                _ => rz * ry * rx,
            };
        }
        public static Matrix4x4 RotateFromEuler(Vector3 eulers, EulerOrder order = EulerOrder.ZYX) => RotateFromEuler(eulers.X, eulers.Y, eulers.Z, order);


        public static Matrix4x4 Translate(Vector3 position)
        {
            return new Matrix4x4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                position.X, position.Y, position.Z, 1.0f
            );
        }
        public static Matrix4x4 Scale(Vector3 scale)
        {
            return new Matrix4x4(
                scale.X, 0.0f, 0.0f, 0.0f,
                0.0f, scale.Y, 0.0f, 0.0f,
                0.0f, 0.0f, scale.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );
        }

        public static Matrix4x4 ModelMatrix(Vector3 position, Vector3 eulerRotation, Vector3 scale) =>
            Scale(scale) * RotateFromEuler(eulerRotation, EulerOrder.ZYX) * Translate(position);

        public enum EulerOrder { XYZ, XZY, YXZ, YZX, ZXY, ZYX }
    }
}
