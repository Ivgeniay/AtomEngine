using System.Collections.Generic;
using Newtonsoft.Json;

namespace Editor
{
    internal class WorldData
    {
        public uint WorldId { get; set; } = 0;
        public string WorldName { get; set; } = "World_0";
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        public List<SystemDescriptor> SystemDescriptors { get; set; } = new List<SystemDescriptor>();
        [JsonIgnore]
        public bool IsDirty { get; set; }
    }

}
