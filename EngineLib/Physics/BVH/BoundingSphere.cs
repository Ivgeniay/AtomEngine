using System.Numerics;

namespace AtomEngine
{
    public struct BoundingSphere
    {
        public Vector3 Position;
        public float Radius;

        public BoundingSphere(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public bool Intersects(in BoundingSphere other)
        {
            return Vector3.Distance(Position, other.Position) < Radius + other.Radius;
        }

        public bool Intersects(in BoundingBox box)
        {
            float x = Math.Max(box.Min.X, Math.Min(Position.X, box.Max.X));
            float y = Math.Max(box.Min.Y, Math.Min(Position.Y, box.Max.Y));
            float z = Math.Max(box.Min.Z, Math.Min(Position.Z, box.Max.Z));
            float cubeDistance = (x - Position.X) * (x - Position.X) + (y - Position.Y) * (y - Position.Y) + (z - Position.Z) * (z - Position.Z);
            return cubeDistance < Radius * Radius;
        }

        public BoundingSphere Transform(Matrix4x4 modelTransformMatrix)
        {
            float scaleX = new Vector3(modelTransformMatrix.M11, modelTransformMatrix.M12, modelTransformMatrix.M13).Length();
            float scaleY = new Vector3(modelTransformMatrix.M21, modelTransformMatrix.M22, modelTransformMatrix.M23).Length();
            float scaleZ = new Vector3(modelTransformMatrix.M31, modelTransformMatrix.M32, modelTransformMatrix.M33).Length();

            float maxScale = MathF.Max(scaleX, MathF.Max(scaleY, scaleZ));
            Vector3 transformedCenter = Vector3.Transform(Position, modelTransformMatrix);

            return new BoundingSphere(transformedCenter, Radius * maxScale);
        }
    }
}
