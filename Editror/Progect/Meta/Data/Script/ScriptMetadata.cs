namespace Editor
{
    internal class ScriptMetadata : AssetMetadata
    {
        public ScriptMetadata() { 
            AssetType = MetadataType.Script;
        }

        public bool IsGenerated { get; set; } = false;
        public string SourceAssetGuid { get; set; } = string.Empty;
    }
}
