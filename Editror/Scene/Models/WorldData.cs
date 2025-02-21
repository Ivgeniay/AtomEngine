using System.Collections.Generic;

namespace Editor
{
    internal class WorldData
    {
        public string WorldName { get; set; } = "World_0";
        public List<EntityData> Entities { get; set; } = new();
        public bool IsDirty { get; set; }
    }
}
