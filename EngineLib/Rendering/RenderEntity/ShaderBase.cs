namespace AtomEngine.RenderEntity
{
    public abstract class ShaderBase : IDisposable
    {
        protected uint handle;
        public uint Handle => handle;
        public abstract void Use();
        public abstract void SetUniform(string name, object value);
        public abstract void Dispose();

        public static implicit operator uint(ShaderBase shader)
        {
            if (shader == null) return 0;
            return shader.handle;
        }
        public static implicit operator int(ShaderBase shader)
        {
            if (shader == null) return 0;
            return (int)shader.handle;
        }
    }
}
