using System.Collections.Generic;
using System.Linq;
using AtomEngine;
using EngineLib;
using OpenglLib;
using System;
using System.Reflection.Metadata;

namespace Editor
{
    internal class MaterialComponentDependencyHandler : ComponentDependencyHandler
    {
        private SceneManager _sceneManager;
        private MaterialAssetManager _materialManager;
        private MetadataManager _metadataManager;
        private Dictionary<string, List<(uint WorldId, uint EntityId)>> _materialUsageCache = new();

        public MaterialComponentDependencyHandler()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            _materialManager = ServiceHub.Get<MaterialAssetManager>();
            _metadataManager = ServiceHub.Get<MetadataManager>();

            _sceneManager.OnSceneInitialize += RebuildMaterialCache;
            _sceneManager.OnComponentAdded += HandleComponentAdded;
            _sceneManager.OnComponentRemoved += HandleComponentRemoved;
            _sceneManager.OnComponentChange += HandleComponentChanged;
        }

        private void RebuildMaterialCache(ProjectScene scene)
        {
            _materialUsageCache.Clear();

            foreach (var world in scene.Worlds)
            {
                foreach (var entity in world.Entities)
                {
                    foreach (var componentKV in entity.Components)
                    {
                        if (componentKV.Value is MaterialComponent)
                        {
                            var materialGuid = GetMaterialGuidFromComponent(componentKV.Value);
                            if (!string.IsNullOrEmpty(materialGuid))
                            {
                                AddToMaterialCache(world.WorldId, entity.Id, materialGuid);
                            }
                        }
                    }
                }
            }
        }

        private void AddToMaterialCache(uint worldId, uint entityId, string materialGuid)
        {
            if (string.IsNullOrEmpty(materialGuid))
                return;

            if (!_materialUsageCache.TryGetValue(materialGuid, out var list))
            {
                list = new List<(uint, uint)>();
                _materialUsageCache[materialGuid] = list;
            }

            if (!list.Any(x => x.WorldId == worldId && x.EntityId == entityId))
            {
                list.Add((worldId, entityId));
            }
        }

        private void RemoveFromMaterialCache(uint worldId, uint entityId, string materialGuid)
        {
            if (string.IsNullOrEmpty(materialGuid))
                return;

            if (_materialUsageCache.TryGetValue(materialGuid, out var list))
            {
                list.RemoveAll(tuple => tuple.WorldId == worldId && tuple.EntityId == entityId);

                if (list.Count == 0)
                {
                    _materialUsageCache.Remove(materialGuid);
                }
            }
        }

        private string GetMaterialGuidFromComponent(IComponent component)
        {
            var type = component.GetType();
            var field = type.GetField("MaterialGUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(component) as string;
            }

            return null;
        }

        private void HandleComponentAdded(uint worldId, uint entityId, IComponent component)
        {
            if (component is MaterialComponent)
            {
                var materialGuid = GetMaterialGuidFromComponent(component);
                if (!string.IsNullOrEmpty(materialGuid))
                {
                    AddToMaterialCache(worldId, entityId, materialGuid);
                }
            }
        }

        private void HandleComponentRemoved(uint worldId, uint entityId, IComponent component)
        {
            if (component is MaterialComponent)
            {
                var materialGuid = GetMaterialGuidFromComponent(component);
                if (!string.IsNullOrEmpty(materialGuid))
                {
                    RemoveFromMaterialCache(worldId, entityId, materialGuid);
                }
            }
        }

        private void HandleComponentChanged(uint worldId, uint entityId, IComponent component, bool ignoreSceneView)
        {
            if (component is MaterialComponent)
            {
                string oldGuid = null;
                foreach (var kv in _materialUsageCache)
                {
                    if (kv.Value.Any(x => x.WorldId == worldId && x.EntityId == entityId))
                    {
                        oldGuid = kv.Key;
                        break;
                    }
                }

                var newGuid = GetMaterialGuidFromComponent(component);

                if (oldGuid != newGuid)
                {
                    if (oldGuid != null)
                    {
                        RemoveFromMaterialCache(worldId, entityId, oldGuid);
                    }

                    if (newGuid != null)
                    {
                        AddToMaterialCache(worldId, entityId, newGuid);
                    }
                }
            }
        }

        public override void HandleDependencyChanged(string assetPath, string changedDependencyPath, FileMetadata dependencyMeta)
        {
            //if (_materialUsageCache.TryGetValue(dependencyMeta.Guid, out var affectedComponents))
            //{
            //    foreach (var (worldId, entityId) in affectedComponents)
            //    {
            //        try
            //        {
            //            if (SceneManager.EntityCompProvider.HasComponent<MaterialComponent>(entityId))
            //            {
            //                ref MaterialComponent materialComponent = ref SceneManager.EntityCompProvider.GetComponent<MaterialComponent>(entityId);
            //                _sceneManager.ComponentChange(entityId, materialComponent, false);
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            DebLogger.Error($"Ошибка при обновлении MaterialComponent (Entity: {entityId}): {ex.Message}");
            //        }
            //    }
            //}
        }

        public override void HandleDependencyDeleted(string assetPath, string deletedDependencyGuid, FileMetadata dependencyMeta)
        {
            if (_materialUsageCache.TryGetValue(dependencyMeta.Guid, out var affectedComponents))
            {
                string defaultMaterialGuid = GetDefaultMaterialGuid();

                foreach (var (worldId, entityId) in affectedComponents)
                {
                    try
                    {
                        if (SceneManager.EntityCompProvider.HasComponent<MaterialComponent>(entityId))
                        {
                            ref MaterialComponent materialComponent = ref SceneManager.EntityCompProvider.GetComponent<MaterialComponent>(entityId);

                            var type = materialComponent.GetType();
                            var field = type.GetField("MaterialGUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (field != null)
                            {
                                TypedReference tr = __makeref(materialComponent);
                                field.SetValueDirect(tr, defaultMaterialGuid);
                                materialComponent.Material = null;
                                _sceneManager.ComponentChange(entityId, materialComponent, false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка при обновлении MaterialComponent (Entity: {entityId}): {ex.Message}");
                    }
                }

                _materialUsageCache.Remove(dependencyMeta.Guid);
            }
        }

        public override void HandleDependencyAdded(string assetPath, string addedDependencyPath, FileMetadata dependencyMeta)
        { }

        public override void HandleDependencyRemoved(string assetPath, string removedDependencyPath, FileMetadata dependencyMeta)
        { }

        private string GetDefaultMaterialGuid()
        {
            var defaultMaterials = _metadataManager.FindAssetsByTag("DefaultMaterial");
            if (defaultMaterials.Count > 0)
            {
                var metadata = _metadataManager.GetMetadata(defaultMaterials[0]);
                return metadata.Guid;
            }

            return string.Empty;
        }
    }

}
