using AtomEngine.RenderEntity;
using System.Numerics;

namespace AtomEngine
{
    public struct BoundingBox: IBoundingVolume
    {
        private Vector3[] _vertices;
        private uint[] _indices;

        private Vector3 _min;
        private Vector3 _max;
        public Vector3 Min { get => _min;  }
        public Vector3 Max { get => _max; }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            this._min = min;
            this._max = max;
        }

        public BoundingBox(MeshBase meshBase)
        {
            var box = BoundingBox.ComputeBoundingBox(meshBase.Vertices_);
            _min = box._min;
            _max = box._max;
        }
        public Vector3[] GetVertices()
        {
            if (_vertices != null) return _vertices;

            _vertices = new Vector3[]
            {
                new Vector3(_min.X, _min.Y, _min.Z),  
                new Vector3(_max.X, _min.Y, _min.Z),  
                new Vector3(_max.X, _min.Y, _max.Z),  
                new Vector3(_min.X, _min.Y, _max.Z),  

                new Vector3(_min.X, _max.Y, _min.Z),  
                new Vector3(_max.X, _max.Y, _min.Z),  
                new Vector3(_max.X, _max.Y, _max.Z),  
                new Vector3(_min.X, _max.Y, _max.Z)   
            };

            return _vertices;
        }

        public uint[] GetIndices()
        {
            if (_indices != null) return _indices;
            _indices = new uint[]
            {
                0, 2, 1,
                0, 3, 2,
         
                4, 5, 6,
                4, 6, 7,
         
                0, 1, 4,
                1, 5, 4,
         
                2, 3, 6,
                3, 7, 6,
         
                1, 2, 5,
                2, 6, 5,
         
                3, 0, 7,
                0, 4, 7 
            };

            return _indices;
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
            corners[0] = new Vector3(_min.X, _min.Y, _min.Z);
            corners[1] = new Vector3(_min.X, _min.Y, _max.Z);
            corners[2] = new Vector3(_min.X, _max.Y, _min.Z);
            corners[3] = new Vector3(_min.X, _max.Y, _max.Z);
            corners[4] = new Vector3(_max.X, _min.Y, _min.Z);
            corners[5] = new Vector3(_max.X, _min.Y, _max.Z);
            corners[6] = new Vector3(_max.X, _max.Y, _min.Z);
            corners[7] = new Vector3(_max.X, _max.Y, _max.Z);

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

        public Vector3 GetCenter() => (_min + _max) * 0.5f;
        public Vector3 GetExtents() => (_max - _min) * 0.5f;

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
            corners[0] = new Vector3(_min.X, _min.Y, _min.Z);
            corners[1] = new Vector3(_min.X, _min.Y, _max.Z);
            corners[2] = new Vector3(_min.X, _max.Y, _min.Z);
            corners[3] = new Vector3(_min.X, _max.Y, _max.Z);
            corners[4] = new Vector3(_max.X, _min.Y, _min.Z);
            corners[5] = new Vector3(_max.X, _min.Y, _max.Z);
            corners[6] = new Vector3(_max.X, _max.Y, _min.Z);
            corners[7] = new Vector3(_max.X, _max.Y, _max.Z);

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
            if (_max.X < other._min.X || _min.X > other._max.X) return false;
            if (_max.Y < other._min.Y || _min.Y > other._max.Y) return false;
            if (_max.Z < other._min.Z || _min.Z > other._max.Z) return false;

            return true;
        }
        private bool Intersects(in BoundingSphere sphere)
        {
            float x = Math.Max(_min.X, Math.Min(sphere.Position.X, _max.X));
            float y = Math.Max(_min.Y, Math.Min(sphere.Position.Y, _max.Y));
            float z = Math.Max(_min.Z, Math.Min(sphere.Position.Z, _max.Z));
            float cubeDistance = (x - sphere.Position.X) * (x - sphere.Position.X) + (y - sphere.Position.Y) * (y - sphere.Position.Y) + (z - sphere.Position.Z) * (z - sphere.Position.Z);
            return cubeDistance < sphere.Radius * sphere.Radius;
        }
    }
}
