using EngineLib.RenderEntity;

namespace EngineLib
{
    public struct ShaderComponent : IComponent, IDisposable
    {
        public Entity Owner { get; }
        public readonly ShaderBase Shader;

        public ShaderComponent(Entity owner, ShaderBase shader)
        {
            Owner = owner;
            Shader = shader;
        }

        public void Dispose()
        {
            if (Shader is IDisposable disposableShader)
            {
                disposableShader.Dispose();
            }
        }
    }
}
