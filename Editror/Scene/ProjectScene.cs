using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    internal class ProjectScene
    {
        public List<WorldData> Worlds = new List<WorldData>(); 
        public string ScenePath { get; set; } = string.Empty;
        private uint _entityIndexator = 0;

        [JsonIgnore] private WorldData _currentWorldData { get; set; }

        public WorldData CurrentWorldData
        {
            get
            {
                if (_currentWorldData == null)
                {
                    if (Worlds.Count == 0)
                    {
                        WorldData newWorld = SceneFileHelper.CreateWorldData();
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
            set
            {
                _currentWorldData = value;
            }
        }
        public ProjectScene(List<WorldData> worlds, WorldData currentWorldData)
        {
            Worlds = worlds;
            _currentWorldData = currentWorldData;
        }
        public bool IsDirty { 
            get => Worlds.Any(e => e.IsDirty);
            private set => CurrentWorldData.IsDirty = value;
        }
        public string WorldName {
            get => CurrentWorldData.WorldName;
            set { CurrentWorldData.WorldName = value; MakeDirty(); } 
        }

        internal void Initialize()
        {
            _entityIndexator = CurrentWorldData.Entities.Max(e => e.Id);
        }

        #region Entity
        internal uint AddEntity(string entityName)
        {
            Entity entity = new Entity(++_entityIndexator, 0);
            EntityData newEntityData = new EntityData()
            {
                Name = entityName,
                Id = entity.Id,
                Version = entity.Version,
            };
            CurrentWorldData.Entities.Add(newEntityData);
            MakeDirty();
            return entity.Id;
        }
        internal void AddDuplicateEntity(EntityHierarchyItem hierarchyEntity)
        {
            Entity entity = new Entity(_entityIndexator++, 0);
            EntityData newEntityData = new EntityData()
            {
                Name = hierarchyEntity.Name,
                Id = entity.Id,
                Version = entity.Version,
            };
            CurrentWorldData.Entities.Add(newEntityData);
            MakeDirty();
        }
        internal uint RenameEntity(EntityHierarchyItem entity)
        {
            var world = CurrentWorldData;
            var editableEntity = world.Entities.Where(e => e.Id == entity.Id && e.Version == entity.Version).First();
            editableEntity.Name = entity.Name;
            MakeDirty();
            return entity.Id;
        }
        internal void DeleteEntity(EntityHierarchyItem entity)
        {
            var world = CurrentWorldData;
            var editableEntity = world.Entities.Where(e => e.Id == entity.Id && e.Version == entity.Version).First();
            world.Entities.Remove(editableEntity);
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

        internal WorldData RemoveWorld(string worldName)
        {
            var world = Worlds.Where(w => w.WorldName == worldName).First();
            Worlds.Remove(world);
            MakeDirty();
            return world;
        }

        internal WorldData CreateWorld(string worldName)
        {
            var newWorldData = new WorldData();
            int maxId = -1;
            if (Worlds.Count > 0)
            {
                maxId = (int)Worlds.Max(e => e.WorldId);
            }

            newWorldData.WorldName = worldName;
            newWorldData.WorldId = (uint)(1 + maxId);
            Worlds.Add(newWorldData);
            MakeDirty();
            return newWorldData;
        }

        internal void SelecteWorld(string worldName)
        {
            var world = Worlds.Where(w => w.WorldName == worldName).First();
            CurrentWorldData = world;
        }


        #endregion

        public void MakeDirty() => IsDirty = true;
        public void MakeUndirty() => Worlds.ForEach(e => e.IsDirty = false);

        private uint GetAvailableId(List<EntityData> entities)
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

        internal object AddComponent(uint entityId, Type typeComponent)
        {
            var entityData = _currentWorldData.Entities.First(e => e.Id == entityId);
            var instanceComponent = Activator.CreateInstance(typeComponent);
            IComponent interfacesDomponent = (IComponent)instanceComponent;
            entityData.Components.Add(typeComponent.Name, interfacesDomponent);
            MakeDirty();
            return instanceComponent;
        }

        internal object RemoveComponent(uint entityId, Type typeComponent)
        {
            var entityData = _currentWorldData.Entities.First(e => e.Id == entityId);
            var component = entityData.Components.FirstOrDefault(e => e.Key == typeComponent.Name).Value;
            entityData.Components.Remove(typeComponent.Name);
            MakeDirty();
            return component;
        }
    }
}
