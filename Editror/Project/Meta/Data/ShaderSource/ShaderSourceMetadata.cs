using System.Collections.Generic;
using EngineLib;

namespace Editor
{
    internal class ShaderSourceMetadata : FileMetadata
    {
        public ShaderSourceMetadata() {
            AssetType = MetadataType.ShaderSource;
        }

        public bool AutoGeneration { get; set; } = false;
        public bool IsGenerator { get; set; } = false;
        public List<string> GeneratedAssets { get; set; } = new List<string>();
    }
}
