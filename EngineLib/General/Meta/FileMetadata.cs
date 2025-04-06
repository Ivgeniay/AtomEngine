using Newtonsoft.Json;

namespace EngineLib
{
    public class FileMetadata
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public MetadataType AssetType { get; set; } = MetadataType.Unknown;
        public bool IsTypeExplicitlySet { get; set; } = false;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> ImportSettings { get; set; } = new Dictionary<string, object>();
        public string ContentHash { get; set; } = string.Empty;

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}
