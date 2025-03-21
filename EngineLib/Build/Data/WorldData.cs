using Newtonsoft.Json;

namespace AtomEngine
{
    public class WorldData
    {
        public uint WorldId { get; set; } = 0;
        public string WorldName { get; set; } = "World_0";
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        [JsonIgnore]
        public bool IsDirty { get; set; }



        public class Refer
        {
            private readonly WorldData _worldData;
            public Refer(WorldData worldData) => _worldData = worldData;
            [JsonProperty]
            public List<ReferenceData> References { get; set; } = new List<ReferenceData>();
        }
    }

    public class ReferenceData
    {
        public uint EntityId = uint.MinValue;
        public string ComponentType = string.Empty;
        public string FieldName = string.Empty;
        public string GUID = string.Empty;
        public string Context = string.Empty;
    }

}
