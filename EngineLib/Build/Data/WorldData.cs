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


    }

}
