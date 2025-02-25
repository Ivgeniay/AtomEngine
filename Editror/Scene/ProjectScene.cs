using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    internal class ProjectScene
    {
        private readonly object _worldLock = new object();

        public List<WorldData1> Worlds = new List<WorldData1>(); 
        public string ScenePath { get; set; } = string.Empty;

        [JsonIgnore] private WorldData1 _currentWorldData { get; set; }
        public WorldData1 CurrentWorldData
        {
            get
            {
                lock (_worldLock)
                {
                    if (_currentWorldData == null)
                    {
                        if (Worlds.Count == 0)
                        {
                            WorldData1 newWorld = SceneFileHelper.CreateNewScene();
                            Worlds.Add(newWorld);
                            _currentWorldData = newWorld;
                        }
                        else
                        {
                            _currentWorldData = Worlds.First();
                        }
                    }
                    return _currentWorldData;
                }
            }
            set
            {
                lock (_worldLock)
                {
                    _currentWorldData = value;
                }
            }
        }
        public ProjectScene(List<WorldData1> worlds, WorldData1 currentWorldData)
        {
            Worlds = worlds;
            _currentWorldData = currentWorldData;
        }
        public bool IsDirty { get => Worlds.Any(e => e.IsDirty); private set => CurrentWorldData.IsDirty = value; }
        public string WorldName { get => CurrentWorldData.WorldName; set { CurrentWorldData.WorldName = value; MakeDirty(); } }


        #region Entity
        internal void AddEntity(string entityName)
        {
            Entity entity = CurrentWorldData.World.CreateEntity();
            EntityData1 newEntityData = new EntityData1()
            {
                Name = entityName,
                Id = entity.Id,
                Version = entity.Version,
            };
            CurrentWorldData.Entities.Add(newEntityData);
            MakeDirty();
        }
        internal void AddDuplicateEntity(EntityHierarchyItem hierarchyEntity)
        {
            Entity entity = CurrentWorldData.World.CreateEntity();
            EntityData1 newEntityData = new EntityData1()
            {
                Name = hierarchyEntity.Name,
                Id = entity.Id,
                Version = entity.Version,
            };
            CurrentWorldData.Entities.Add(newEntityData);
            MakeDirty();
        }

        internal void RenameEntity(EntityHierarchyItem entity)
        {
            var world = CurrentWorldData;
            var editableEntity = world.Entities.Where(e => e.Id == entity.Id && e.Version == entity.Version).First();
            editableEntity.Name = entity.Name;
            MakeDirty();
        }

        internal void DeleteEntity(EntityHierarchyItem entity)
        {
            var world = CurrentWorldData;
            var editableEntity = world.Entities.Where(e => e.Id == entity.Id && e.Version == entity.Version).First();
            world.Entities.Remove(editableEntity);
            CurrentWorldData.World.DestroyEntity(entity.Id, entity.Version);
            MakeDirty();
        }
        #endregion

        #region World
        internal void RenameWorld((string, string) worldNameLastCurrent)
        {
            var world = Worlds.Where(w => w.WorldName == worldNameLastCurrent.Item1).First();
            world.WorldName = worldNameLastCurrent.Item2;
            MakeDirty();
        }

        internal void RemoveWorld(string worldName)
        {
            var world = Worlds.Where(w => w.WorldName == worldName).First();
            Worlds.Remove(world);
            MakeDirty();
        }

        internal void CreateWorld(string worldName)
        {
            var newWorldData = new WorldData1();
            newWorldData.WorldName = worldName;
            Worlds.Add(newWorldData);
            MakeDirty();
        }

        internal void SelecteWorld(string worldName)
        {
            var world = Worlds.Where(w => w.WorldName == worldName).First();
            CurrentWorldData = world;
            MakeDirty();
        }


        #endregion

        public void MakeDirty() => IsDirty = true;
        public void MakeUndirty() => Worlds.ForEach(e => e.IsDirty = false);

        private uint GetAvailableId(List<EntityData1> entities)
        {
            var sortedEntities = entities.OrderBy(e => e.Id).ToList();

            uint expectedId = 0;
            foreach (var entity in sortedEntities)
            {
                if (entity.Id != expectedId)
                {
                    return expectedId;
                }
                expectedId++;
            }

            return expectedId;
        }
    }
}
