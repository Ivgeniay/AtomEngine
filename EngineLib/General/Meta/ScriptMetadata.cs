namespace EngineLib
{
    public class ScriptMetadata : FileMetadata
    {
        public ScriptMetadata()
        {
            AssetType = MetadataType.Script;
        }

        public bool IsGenerated { get; set; } = false;
        public string SourceAssetGuid { get; set; } = string.Empty;
        public List<string> Types = new List<string>();
    }
}
