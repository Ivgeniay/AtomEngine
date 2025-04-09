using EngineLib;

namespace AtomEngine.RenderEntity
{
    public abstract class MeshBase : IDisposable
    {
        public float[] Vertices { get; protected set; }
        public uint[] Indices { get; protected set; }
        public Triangle[] Triangles { get; protected set; }
        public Vertex[] Vertices_ { get; protected set; }

        public IBoundingVolume BoundingVolume { get; protected set; }

        public abstract void Dispose();
        public abstract void Draw();
        public abstract void Draw(ShaderBase shaderBase);
    }
}
