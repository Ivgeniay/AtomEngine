using EngineLib;

namespace OpenglLib
{
    public class ShaderMetadata : FileMetadata
    {
        public ShaderMetadata() {
            AssetType = MetadataType.Shader;
        }

        public string ShaderType { get; set; } = string.Empty;

    }
}
