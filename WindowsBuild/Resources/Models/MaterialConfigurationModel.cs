namespace WindowsBuild
{
    public class MaterialConfigurationModel
    {
        public string ShaderRepresentationTypeName { get; set; } = string.Empty;

        public Dictionary<string, object> UniformValues { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> TextureReferences { get; set; } = new Dictionary<string, string>();
    }
}
