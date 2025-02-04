namespace EngineLib.RenderEntity
{
    public abstract class ShaderBase : IDisposable
    {
        public abstract void Dispose();
        public abstract void SetUniform(string name, object value);
        public abstract void Use();
    }
}
