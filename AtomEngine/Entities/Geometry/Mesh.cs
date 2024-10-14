using AtomEngine.Math;

namespace AtomEngine
{
    public sealed class Mesh
    {
        public List<Vector3D<double>>? Vertices;
        public List<int>? Triangles;
        public List<Color4>? Colors;

        public List<Vector2D<double>>? Uv;
        public List<Vector2D<double>>? Uv2;
        public List<Vector2D<double>>? Uv3;
        public List<Vector2D<double>>? Uv4;
        public List<Vector2D<double>>? Uv5;
        public List<Vector2D<double>>? Uv6;
        public List<Vector2D<double>>? Uv7;
        public List<Vector2D<double>>? Uv8;
        public int VertexCount => Vertices?.Count ?? 0;

        public void Clear()
        {
            Vertices = null;
            Uv = null;
            Triangles = null;
        }
    }
}
