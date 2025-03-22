using EngineLib;

namespace OpenglLib
{
    public class MaterialAsset : Asset
    {
        public string ShaderRepresentationGuid { get; set; } = string.Empty;
        public string ShaderRepresentationTypeName { get; set; } = string.Empty;

        public Dictionary<string, object> UniformValues { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> TextureReferences { get; set; } = new Dictionary<string, string>();
        public override string Name { get; set; } = "New Material";


        public virtual void SetUniformValue(string name, object value)
        {
            object serializedValue = value;
            UniformValues[name] = serializedValue;
            ServiceHub.Get<MaterialCacher>().SaveMaterial(this);
            ServiceHub.Get<MaterialFactory>().ApplyUniformValues(Guid, UniformValues);
        }

        public virtual void SetTexture(string samplerName, string textureGuid)
        {
            TextureReferences[samplerName] = textureGuid;
            ServiceHub.Get<MaterialCacher>().SaveMaterial(this);
            ServiceHub.Get<MaterialFactory>().ApplyTextures(Guid, TextureReferences);
        }

    }
}
