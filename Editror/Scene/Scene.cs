using System.Collections.Generic;
using Newtonsoft.Json;
using AtomEngine;

namespace Editor
{
    internal class Scene
    {
        public List<WorldData> Worlds = new List<WorldData>(); 
        public string ScenePath { get; set; }
        [JsonIgnore]
        private WorldData _currentWorldData { get; set; }
        public WorldData CurrentWorldData { get
            {
                if (_currentWorldData == null) _currentWorldData = Worlds[0];
                return _currentWorldData;
            }
            set { _currentWorldData = value; }
        }
        public Scene() { }
        public Scene(string sourceWorldsData) { }
        public Scene(List<WorldData> worlds, WorldData currentWorldData)
        {
            Worlds = worlds;
            _currentWorldData = currentWorldData;
        }

        public bool IsDirty { get => CurrentWorldData.IsDirty; private set => CurrentWorldData.IsDirty = value; }
        public string WorldName { get => CurrentWorldData.WorldName; set { CurrentWorldData.WorldName = value; MakeDirty(); } }
        

        public void AddEntity(EntityData entityData)
        {
            if (!CurrentWorldData.Entities.Contains(entityData))
            {
                CurrentWorldData.Entities.Add(entityData);
                MakeDirty();
            }
        }

        public void RemoveEntity(EntityData entityData)
        {
            if (!CurrentWorldData.Entities.Contains(entityData))
            {
                CurrentWorldData.Entities.Remove(entityData);
                MakeDirty();
            }
        }

        public void UpdateEntity(EntityData entityData)
        {

        }

        public void MakeDirty() => IsDirty = true;
    }
}
