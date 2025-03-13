using System.Collections.Generic;
using Newtonsoft.Json;

namespace Editor
{
    internal class EditorWorldData
    {
        public uint WorldId { get; set; } = 0;
        public string WorldName { get; set; } = "World_0";
        public List<EditorEntityData> Entities { get; set; } = new List<EditorEntityData>();

        [JsonIgnore]
        public bool IsDirty { get; set; }
    }
}
