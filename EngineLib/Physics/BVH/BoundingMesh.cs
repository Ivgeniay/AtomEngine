using AtomEngine.RenderEntity;
using System.Numerics;

namespace AtomEngine
{
    public struct BoundingMesh : IBoundingVolume
    {
        private Vector3[] _vertices;

        private Vector3 _min;
        private Vector3 _max;
        public Vector3 Min => _min;
        public Vector3 Max => _max;

        public Vector3[] GetVertices() => _vertices;
        public uint[] GetIndices() { return null; }
        public BoundingMesh(MeshBase mesh)
        {
            List<Vector3> meshVertices = new List<Vector3>();
            foreach (var vertex in mesh.Vertices_)
            {
                meshVertices.Add(vertex.Position);
            }
            _vertices = meshVertices.ToArray();
            CalculateBounds();
        }
        public BoundingMesh(Vector3[] meshVertices)
        {
            _vertices = meshVertices;
            CalculateBounds();
        }

        public bool Intersects(IBoundingVolume other)
        {
            if (Max.X < other.Min.X || Min.X > other.Max.X ||
                Max.Y < other.Min.Y || Min.Y > other.Max.Y ||
                Max.Z < other.Min.Z || Min.Z > other.Max.Z)
            {
                return false;
            }

            return false;
            //return GJKAlgorithm.Intersect(_vertices, other.GetVertices());
        }

        public IBoundingVolume Transform(Matrix4x4 modelMatrix)
        {
            Vector3[] transformedVertices = new Vector3[_vertices.Length];

            for (int i = 0; i < _vertices.Length; i++)
            {
                transformedVertices[i] = Vector3.Transform(_vertices[i], modelMatrix);
            }

            // Создаем новый BoundingMesh с трансформированными вершинами
            return new BoundingMesh(transformedVertices);
        }

        private void CalculateBounds()
        {
            _min = new Vector3(float.MaxValue);
            _max = new Vector3(float.MinValue);

            foreach (var vertex in _vertices)
            {
                _min = Vector3.Min(_min, vertex);
                _max = Vector3.Max(_max, vertex);
            }
        }
    }
}
