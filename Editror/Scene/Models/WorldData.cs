using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using System.Linq;
using System;
using AtomEngine;

namespace Editor
{
    internal class WorldData
    {
        public string WorldName { get; set; } = "World_0";
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        public List<SystemDescriptor> SystemDescriptors { get; set; } = new List<SystemDescriptor>();
        [JsonIgnore]
        public bool IsDirty { get; set; }
    }

}
