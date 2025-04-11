using System.Collections.Generic;
using EngineLib;

namespace Editor
{
    internal class ShaderSourceMetadata : FileMetadata
    {
        public ShaderSourceMetadata() {
            AssetType = MetadataType.ShaderSource;
        }
    }
}
