using AtomEngine.RenderEntity;
using System.Numerics;

namespace AtomEngine
{
    public struct BoundingBox: IBoundingVolume
    {
        private Vector3 min;
        private Vector3 max;
        public Vector3 Min { get => min;  }
        public Vector3 Max { get => max; }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            this.min = min;
            this.max = max;
        }

        public BoundingBox(MeshBase meshBase)
        {
            var box = BoundingBox.ComputeBoundingBox(meshBase.Vertices_);
            min = box.min;
            max = box.max;
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

        public BoundingBox Transform2(Matrix4x4 modelTransformMatrix)
        {
            var corners = new Vector3[8];
            corners[0] = new Vector3(min.X, min.Y, min.Z);
            corners[1] = new Vector3(min.X, min.Y, max.Z);
            corners[2] = new Vector3(min.X, max.Y, min.Z);
            corners[3] = new Vector3(min.X, max.Y, max.Z);
            corners[4] = new Vector3(max.X, min.Y, min.Z);
            corners[5] = new Vector3(max.X, min.Y, max.Z);
            corners[6] = new Vector3(max.X, max.Y, min.Z);
            corners[7] = new Vector3(max.X, max.Y, max.Z);

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

        public Vector3 GetCenter() => (min + max) * 0.5f;
        public Vector3 GetExtents() => (max - min) * 0.5f;

        public bool Intersects(IBoundingVolume other) => other switch
        {
            BoundingBox box => this.Intersects(in box),
            BoundingSphere sphere => this.Intersects(in sphere),
            BoundingComponent component => this.Intersects(component.BoundingVolume),
            _ => throw new ArgumentError(nameof(Intersects) + " with " + $"{other}"),
        };

        public IBoundingVolume Transform(Matrix4x4 modelTransformMatrix)
        {
            var corners = new Vector3[8];
            corners[0] = new Vector3(min.X, min.Y, min.Z);
            corners[1] = new Vector3(min.X, min.Y, max.Z);
            corners[2] = new Vector3(min.X, max.Y, min.Z);
            corners[3] = new Vector3(min.X, max.Y, max.Z);
            corners[4] = new Vector3(max.X, min.Y, min.Z);
            corners[5] = new Vector3(max.X, min.Y, max.Z);
            corners[6] = new Vector3(max.X, max.Y, min.Z);
            corners[7] = new Vector3(max.X, max.Y, max.Z);

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

        private bool Intersects(in BoundingBox other)
        {
            if (max.X < other.min.X || min.X > other.max.X) return false;
            if (max.Y < other.min.Y || min.Y > other.max.Y) return false;
            if (max.Z < other.min.Z || min.Z > other.max.Z) return false;

            return true;
        }
        private bool Intersects(in BoundingSphere sphere)
        {
            float x = Math.Max(min.X, Math.Min(sphere.Position.X, max.X));
            float y = Math.Max(min.Y, Math.Min(sphere.Position.Y, max.Y));
            float z = Math.Max(min.Z, Math.Min(sphere.Position.Z, max.Z));
            float cubeDistance = (x - sphere.Position.X) * (x - sphere.Position.X) + (y - sphere.Position.Y) * (y - sphere.Position.Y) + (z - sphere.Position.Z) * (z - sphere.Position.Z);
            return cubeDistance < sphere.Radius * sphere.Radius;
        }
    }
}
