using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using AtomEngine.RenderEntity;
using System.Xml.Linq;

namespace AtomEngine
{
    public partial struct ShaderComponent : IComponent, IDisposable
    {
        public Entity Owner { get; }

        public readonly ShaderBase Shader;
        private string ShaderGUID;

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

    //public partial struct ShaderComponent : IDisposable
    //{
    //    private string _shaderGuid;

    //    [JsonConstructor]
    //    internal ShaderComponent(Entity owner, string shaderGuid)
    //    {
    //        _shaderGuid = shaderGuid;
    //        Shader = null; // Будет загружено позже
    //    }

    //    [OnSerializing]
    //    private void OnSerializing(StreamingContext context)
    //    {
    //        if (Shader != null)
    //            _shaderGuid = Shader.Guid;
    //    }

    //    internal void LoadResources(ResourceManager resourceManager)
    //    {
    //        if (!string.IsNullOrEmpty(_shaderGuid) && Shader == null)
    //            Shader = resourceManager.GetResource<ShaderBase>(_shaderGuid);
    //    }
    //}
}

public class EditorResourceManager
{

}
