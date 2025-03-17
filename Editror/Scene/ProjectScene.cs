using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;
using EngineLib;

namespace Editor
{
    internal class ProjectScene : ProjectSceneData
    {
        public string ScenePath { get; set; } = string.Empty;

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

        #region Entity
        internal uint AddEntity(string entityName)
        {
            var index = GetAvailableId(CurrentWorldData.Entities);
            Entity entity = new Entity(index, 0);
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
        internal uint AddDuplicateEntity(EntityHierarchyItem hierarchyEntity)
        {
            var index = GetAvailableId(CurrentWorldData.Entities);
            var ivaliableName = hierarchyEntity.Name + " (Duplicate)";
            Entity entity = new Entity(index, 0);
            EntityData newEntityData = new EntityData()
            {
                Name = ivaliableName,
                Id = index,
                Version = 0,
            };
            CurrentWorldData.Entities.Add(newEntityData);
            MakeDirty();
            return index;
        }
        internal uint RenameEntity(EntityHierarchyItem entity)
        {
            var world = CurrentWorldData;
            var editableEntity = world.Entities.Where(e => e.Id == entity.Id && e.Version == entity.Version).First();
            editableEntity.Name = entity.Name;
            MakeDirty();
            return entity.Id;
        }
        internal uint DeleteEntity(EntityHierarchyItem entity)
        {
            var world = CurrentWorldData;
            var editableEntity = world.Entities.Where(e => e.Id == entity.Id && e.Version == entity.Version).First();
            world.Entities.Remove(editableEntity);
            MakeDirty();
            return entity.Id;
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

       
        internal object AddComponent(uint entityId, Type typeComponent)
        {
            var entityData = _currentWorldData.Entities.First(e => e.Id == entityId);
            var instanceComponent = Activator.CreateInstance(typeComponent);
            //var instanceComponent = UserAssemblyObjectFactory.CreateInstance(typeComponent);

            var fields = typeComponent.GetFields();
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                if (fieldType == typeof(int) ||
                    fieldType == typeof(long) ||
                    fieldType == typeof(float) ||
                    fieldType == typeof(Single) ||
                    fieldType == typeof(double)) { } else { continue; }

                var attributes = field.GetCustomAttributes(false);
                var minAtributes_ = attributes.FirstOrDefault(e => e.GetType() == typeof(MinAttribute));
                if (minAtributes_ != null && minAtributes_ is MinAttribute minValue)
                {
                    var value = Convert.ChangeType(minValue.MinValue, fieldType);
                    field.SetValue(instanceComponent, value);
                }
            }

            if (typeComponent == typeof(HierarchyComponent))
            {
                if (!entityData.Components.TryGetValue(typeof(TransformComponent).FullName, out var component)) { 
                    var transform = Activator.CreateInstance(typeof(TransformComponent));
                    entityData.Components.Add(typeof(TransformComponent).FullName, (TransformComponent)transform);

                }

            }

            IComponent interfacesComponent = (IComponent)instanceComponent;
            entityData.Components.Add(typeComponent.FullName, interfacesComponent);
            MakeDirty();
            return instanceComponent;
        }
        internal object RemoveComponent(uint entityId, Type typeComponent)
        {
            var entityData = _currentWorldData.Entities.First(e => e.Id == entityId);
            var component = entityData.Components.FirstOrDefault(e => e.Key == typeComponent.FullName).Value;
            entityData.Components.Remove(typeComponent.FullName);
            MakeDirty();
            return component;
        }


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

        private string GetUniqueName(string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (CurrentWorldData.Entities.Any(e => e.Name == name))
            {
                name = $"{baseName} ({counter})";
                counter++;
            }

            return name;
        }

    }
}
