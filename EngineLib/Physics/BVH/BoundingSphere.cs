using System.Numerics;

namespace AtomEngine
{
    public struct BoundingSphere : IBoundingVolume
    {
        public Vector3 Position;
        public float Radius;
        public Vector3 Min => Position - new Vector3(Radius);
        public Vector3 Max => Position + new Vector3(Radius);

        public BoundingSphere(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public static BoundingSphere ComputeBoundingSphere(Vertex[] vertices)
        {
            if (vertices.Length == 0)
                return new BoundingSphere(Vector3.Zero, 0);
            Vector3 center = Vector3.Zero;
            foreach (var vertex in vertices)
            {
                center += vertex.Position;
            }
            center /= vertices.Length;

            float maxRadiusSq = 0;
            foreach (var vertex in vertices)
            {
                float distSq = Vector3.DistanceSquared(center, vertex.Position);
                if (distSq > maxRadiusSq)
                    maxRadiusSq = distSq;
            }

            return new BoundingSphere(center, MathF.Sqrt(maxRadiusSq));
        }

        public static BoundingSphere ComputeBoundingSphereRitter(Vertex[] vertices)
        {
            if (vertices.Length == 0)
                return new BoundingSphere(Vector3.Zero, 0);

            // 1. Находим две самые дальние точки
            Vector3 p1 = vertices[0].Position;
            Vector3 p2 = p1;
            float maxDistSq = 0;

            // Находим самую дальнюю точку от p1
            foreach (var vertex in vertices)
            {
                float distSq = Vector3.DistanceSquared(p1, vertex.Position);
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    p2 = vertex.Position;
                }
            }

            // Находим самую дальнюю точку от p2
            maxDistSq = 0;
            foreach (var vertex in vertices)
            {
                float distSq = Vector3.DistanceSquared(p2, vertex.Position);
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    p1 = vertex.Position;
                }
            }

            // 2. Используем эти точки как начальную сферу
            Vector3 center = (p1 + p2) * 0.5f;
            float radius = Vector3.Distance(p1, p2) * 0.5f;

            // 3. Расширяем сферу чтобы включить все точки
            foreach (var vertex in vertices)
            {
                Vector3 d = vertex.Position - center;
                float dist = d.Length();
                if (dist > radius)
                {
                    float newRadius = (radius + dist) * 0.5f;
                    float k = (newRadius - radius) / dist;
                    radius = newRadius;
                    center += d * k;
                }
            }

            return new BoundingSphere(center, radius);
        }

        public bool Intersects(IBoundingVolume other) => other switch
        {
            BoundingBox box => this.Intersects(in box),
            BoundingSphere sphere => this.Intersects(in sphere),
            _ => throw new ArgumentError(nameof(Intersects) + " with " + $"{other}"),
        };

        public IBoundingVolume Transform(Matrix4x4 modelTransformMatrix)
        {
            float scaleX = new Vector3(modelTransformMatrix.M11, modelTransformMatrix.M12, modelTransformMatrix.M13).Length();
            float scaleY = new Vector3(modelTransformMatrix.M21, modelTransformMatrix.M22, modelTransformMatrix.M23).Length();
            float scaleZ = new Vector3(modelTransformMatrix.M31, modelTransformMatrix.M32, modelTransformMatrix.M33).Length();

            float maxScale = MathF.Max(scaleX, MathF.Max(scaleY, scaleZ));
            Vector3 transformedCenter = Vector3.Transform(Position, modelTransformMatrix);

            return new BoundingSphere(transformedCenter, Radius * maxScale);
        }

        private bool Intersects(in BoundingSphere other)
        {
            return Vector3.Distance(Position, other.Position) < Radius + other.Radius;
        }

        private bool Intersects(in BoundingBox box)
        {
            float x = Math.Max(box.Min.X, Math.Min(Position.X, box.Max.X));
            float y = Math.Max(box.Min.Y, Math.Min(Position.Y, box.Max.Y));
            float z = Math.Max(box.Min.Z, Math.Min(Position.Z, box.Max.Z));
            float cubeDistance = (x - Position.X) * (x - Position.X) + (y - Position.Y) * (y - Position.Y) + (z - Position.Z) * (z - Position.Z);
            return cubeDistance < Radius * Radius;
        }
    }
}
