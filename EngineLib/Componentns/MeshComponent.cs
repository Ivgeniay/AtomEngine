using AtomEngine.RenderEntity;

namespace AtomEngine
{
    public struct MeshComponent : IComponent, IDisposable
    {
        public Entity Owner { get; }
        public readonly MeshBase Mesh;

        public MeshComponent(Entity owner, MeshBase mesh)
        {
            Owner = owner;
            Mesh = mesh;
        }

        public void Dispose()
        {
            if (Mesh is IDisposable disposableMesh)
            {
                disposableMesh.Dispose();
            }
        }
    }
}
