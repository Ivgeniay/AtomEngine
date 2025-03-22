using Newtonsoft.Json;

namespace EngineLib
{
    public abstract class Asset
    {
        [JsonProperty]
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public virtual string Name { get; set; } = "Asset";
    }
}
