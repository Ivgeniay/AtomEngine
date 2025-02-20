using System.Collections.Generic;

namespace Editor
{
    internal class SceneData
    {
        public string SceneName { get; set; } = "Scene_0";
        public List<EntityData> Entities { get; set; } = new();
    }
}
