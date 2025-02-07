namespace AtomEngine.RenderEntity
{
    public abstract class MeshBase : IDisposable
    {
        public abstract void Dispose();
        public abstract void Draw(ShaderBase shaderBase);
    }
}
