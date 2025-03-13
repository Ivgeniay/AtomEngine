using System.Collections.Generic;
using Newtonsoft.Json;
using AtomEngine;

namespace Editor
{
    internal class EditorProjectScene
    {
        public List<EditorWorldData> Worlds = new List<EditorWorldData>();
        public List<SystemData> Systems = new List<SystemData>();
        public string ScenePath { get; set; } = string.Empty;

        [JsonIgnore]
        public EditorWorldData CurrentWorld { get; set; }
    }
}
