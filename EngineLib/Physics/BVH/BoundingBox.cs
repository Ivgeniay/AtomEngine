using AtomEngine.RenderEntity;
using System.Numerics;

namespace AtomEngine
{
    public struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public BoundingBox(MeshBase meshBase)
        {
            var box = BoundingBox.ComputeBoundingBox(meshBase.Vertices_);
            Min = box.Min;
            Max = box.Max;
        }

        public static BoundingBox ComputeBoundingBox(Vertex[] vertices)
        {
            if (vertices.Length == 0)
                return new BoundingBox(Vector3.Zero, Vector3.Zero);

            var min = vertices[0].Position;
            var max = vertices[0].Position;

            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i].Position);
                max = Vector3.Max(max, vertices[i].Position);
            }

            return new BoundingBox(min, max);
        }

        public static BoundingBox ComputeBoundingBox(Vector3[] vertices)
        {
            if (vertices.Length == 0)
                return new BoundingBox(Vector3.Zero, Vector3.Zero);

            var min = vertices[0];
            var max = vertices[0];

            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }

            return new BoundingBox(min, max);
        }

        public BoundingBox Transform(Matrix4x4 modelTransformMatrix)
        {
            var corners = new Vector3[8];
            corners[0] = new Vector3(Min.X, Min.Y, Min.Z);
            corners[1] = new Vector3(Min.X, Min.Y, Max.Z);
            corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
            corners[3] = new Vector3(Min.X, Max.Y, Max.Z);
            corners[4] = new Vector3(Max.X, Min.Y, Min.Z);
            corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
            corners[6] = new Vector3(Max.X, Max.Y, Min.Z);
            corners[7] = new Vector3(Max.X, Max.Y, Max.Z);

            var transformedMin = Vector3.Transform(corners[0], modelTransformMatrix);
            var transformedMax = transformedMin;

            for (int i = 1; i < 8; i++)
            {
                var transformed = Vector3.Transform(corners[i], modelTransformMatrix);
                transformedMin = Vector3.Min(transformedMin, transformed);
                transformedMax = Vector3.Max(transformedMax, transformed);
            }

            return new BoundingBox(transformedMin, transformedMax);
        }

        public Vector3 GetCenter() => (Min + Max) * 0.5f;
        public Vector3 GetExtents() => (Max - Min) * 0.5f;

        public bool Intersects(in BoundingBox other)
        {
            if (Max.X < other.Min.X || Min.X > other.Max.X) return false;
            if (Max.Y < other.Min.Y || Min.Y > other.Max.Y) return false;
            if (Max.Z < other.Min.Z || Min.Z > other.Max.Z) return false;

            return true;
        }

        public bool Intersects(in BoundingSphere sphere)
        {
            float x = Math.Max(Min.X, Math.Min(sphere.Position.X, Max.X));
            float y = Math.Max(Min.Y, Math.Min(sphere.Position.Y, Max.Y));
            float z = Math.Max(Min.Z, Math.Min(sphere.Position.Z, Max.Z));
            float cubeDistance = (x - sphere.Position.X) * (x - sphere.Position.X) + (y - sphere.Position.Y) * (y - sphere.Position.Y) + (z - sphere.Position.Z) * (z - sphere.Position.Z);
            return cubeDistance < sphere.Radius * sphere.Radius;
        }
    }
}
