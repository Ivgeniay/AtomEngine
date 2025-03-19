using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;
using EngineLib;
using System.Reflection;

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

            var fields = typeComponent.GetFields();
            AddComponentValidation(typeComponent, entityData, instanceComponent, fields);

            IComponent interfacesComponent = (IComponent)instanceComponent;
            entityData.Components.Add(typeComponent.FullName, interfacesComponent);
            MakeDirty();
            return instanceComponent;
        }

        private void AddComponentValidation(Type typeComponent, EntityData entityData, object? instanceComponent, FieldInfo[] fields)
        {
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                var attributes = field.GetCustomAttributes(false);

                if (fieldType == typeof(Boolean))
                {
                    var defaultValueAttr = attributes.FirstOrDefault(a => a is DefaultValueAttribute);
                    if (defaultValueAttr != null)
                    {
                        if (defaultValueAttr is DefaultBoolAttribute boolAttr)
                        {
                            var value = Convert.ChangeType(boolAttr.Value, fieldType);
                            field.SetValue(instanceComponent, value);
                        }
                    }
                }

                if (fieldType == typeof(int) || fieldType == typeof(long) ||
                    fieldType == typeof(float) || fieldType == typeof(Single) ||
                    fieldType == typeof(double))
                {
                    var defaultValueAttr = attributes.FirstOrDefault(a => a is DefaultValueAttribute);
                    if (defaultValueAttr != null)
                    {
                        if (defaultValueAttr is DefaultIntAttribute intAttr &&
                            (fieldType == typeof(int) || fieldType == typeof(long)))
                        {
                            var value = Convert.ChangeType(intAttr.Value, fieldType);
                            field.SetValue(instanceComponent, value);
                        }
                        else if (defaultValueAttr is DefaultFloatAttribute floatAttr &&
                                 (fieldType == typeof(float) || fieldType == typeof(Single) || fieldType == typeof(double)))
                        {
                            var value = Convert.ChangeType(floatAttr.Value, fieldType);
                            field.SetValue(instanceComponent, value);
                        }
                    }

                    var rangeAttr = attributes.FirstOrDefault(a => a is RangeAttribute) as RangeAttribute;
                    if (rangeAttr != null)
                    {
                        var currentValue = Convert.ToDouble(field.GetValue(instanceComponent));

                        if (currentValue > rangeAttr.MaxValue)
                        {
                            field.SetValue(instanceComponent, Convert.ChangeType(rangeAttr.MaxValue, fieldType));
                        }
                        else if (currentValue < rangeAttr.MinValue)
                        {
                            field.SetValue(instanceComponent, Convert.ChangeType(rangeAttr.MinValue, fieldType));
                        }
                    }

                    var maxAttr = attributes.FirstOrDefault(a => a is MaxAttribute) as MaxAttribute;
                    if (maxAttr != null)
                    {
                        var currentValue = Convert.ToDouble(field.GetValue(instanceComponent));

                        if (currentValue > maxAttr.MaxValue)
                        {
                            field.SetValue(instanceComponent, Convert.ChangeType(maxAttr.MaxValue, fieldType));
                        }
                    }

                    var minAttr = attributes.FirstOrDefault(a => a is MinAttribute) as MinAttribute;
                    if (minAttr != null)
                    {
                        var currentValue = Convert.ToDouble(field.GetValue(instanceComponent));

                        if (currentValue < minAttr.MinValue)
                        {
                            field.SetValue(instanceComponent, Convert.ChangeType(minAttr.MinValue, fieldType));
                        }
                    }
                }
                else if (fieldType == typeof(string))
                {
                    var defaultStringAttr = attributes.FirstOrDefault(a => a is DefaultStringAttribute) as DefaultStringAttribute;
                    if (defaultStringAttr != null)
                    {
                        field.SetValue(instanceComponent, defaultStringAttr.Value);
                    }
                }
                else if (fieldType.FullName?.Contains("Vector2") == true)
                {
                    var defaultVec2Attr = attributes.FirstOrDefault(a => a is DefaultVector2Attribute) as DefaultVector2Attribute;
                    if (defaultVec2Attr != null)
                    {
                        var vector2Instance = Activator.CreateInstance(fieldType,
                            new object[] { defaultVec2Attr.XValue, defaultVec2Attr.YValue });
                        field.SetValue(instanceComponent, vector2Instance);
                    }
                }
                else if (fieldType.FullName?.Contains("Vector3") == true)
                {
                    var defaultVec3Attr = attributes.FirstOrDefault(a => a is DefaultVector3Attribute) as DefaultVector3Attribute;
                    if (defaultVec3Attr != null)
                    {
                        var vector3Instance = Activator.CreateInstance(fieldType,
                            new object[] { defaultVec3Attr.XValue, defaultVec3Attr.YValue, defaultVec3Attr.ZValue });
                        field.SetValue(instanceComponent, vector3Instance);
                    }
                }
                else if (fieldType.FullName?.Contains("Vector4") == true)
                {
                    var defaultVec4Attr = attributes.FirstOrDefault(a => a is DefaultVector4Attribute) as DefaultVector4Attribute;
                    if (defaultVec4Attr != null)
                    {
                        var vector4Instance = Activator.CreateInstance(fieldType,
                            new object[] { defaultVec4Attr.XValue, defaultVec4Attr.YValue,
                                   defaultVec4Attr.ZValue, defaultVec4Attr.AValue });
                        field.SetValue(instanceComponent, vector4Instance);
                    }
                }
            }

            if (typeComponent == typeof(HierarchyComponent))
            {
                if (!entityData.Components.TryGetValue(typeof(TransformComponent).FullName, out var component))
                {
                    AddComponent(entityData.Id, typeof(TransformComponent));
                }
            }
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
