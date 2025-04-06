using EngineLib;

namespace OpenglLib
{
    public class MaterialAsset : Asset
    {
        public string ShaderRepresentationGuid { get; set; } = string.Empty;
        public string ShaderRepresentationTypeName { get; set; } = string.Empty;
        public bool HasValidShader => !string.IsNullOrEmpty(ShaderRepresentationGuid);

        public Dictionary<string, object> UniformValues { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> TextureReferences { get; set; } = new Dictionary<string, string>();
        public override string Name { get; set; } = "New Material";
    }
}
