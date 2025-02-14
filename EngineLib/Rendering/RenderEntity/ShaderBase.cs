namespace AtomEngine.RenderEntity
{
    public abstract class ShaderBase : IDisposable
    {
        public abstract void Use();
        public abstract void SetUniform(string name, object value);
        public abstract void Dispose();
    }
}
