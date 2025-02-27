using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Editor
{
    public class AssetMetadata
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public MetadataType AssetType { get; set; } = MetadataType.Unknown;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> ImportSettings { get; set; } = new Dictionary<string, object>();
        public string ContentHash { get; set; } = string.Empty;

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}
