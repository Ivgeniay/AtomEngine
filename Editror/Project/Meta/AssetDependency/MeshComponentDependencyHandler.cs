using System.Collections.Generic;
using System.Linq;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    internal class MeshComponentDependencyHandler : ComponentDependencyHandler
    {
        private SceneManager _sceneManager;
        private MetadataManager _metadataManager;
        private Dictionary<string, List<(uint WorldId, uint EntityId)>> _meshUsageCache = new();

        public MeshComponentDependencyHandler()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            _metadataManager = ServiceHub.Get<MetadataManager>();

            _sceneManager.OnSceneInitialize += RebuildMeshCache;
            _sceneManager.OnComponentAdded += HandleComponentAdded;
            _sceneManager.OnComponentRemoved += HandleComponentRemoved;
            _sceneManager.OnComponentChange += HandleComponentChanged;
        }

        private void RebuildMeshCache(ProjectScene scene)
        {
            _meshUsageCache.Clear();

            foreach (var world in scene.Worlds)
            {
                foreach (var entity in world.Entities)
                {
                    foreach (var componentKV in entity.Components)
                    {
                        if (componentKV.Value is MeshComponent)
                        {
                            var meshGuid = GetMeshGuidFromComponent(componentKV.Value);
                            if (!string.IsNullOrEmpty(meshGuid))
                            {
                                AddToMeshCache(world.WorldId, entity.Id, meshGuid);
                            }
                        }
                    }
                }
            }
        }

        private void AddToMeshCache(uint worldId, uint entityId, string meshGuid)
        {
            if (string.IsNullOrEmpty(meshGuid))
                return;

            if (!_meshUsageCache.TryGetValue(meshGuid, out var list))
            {
                list = new List<(uint, uint)>();
                _meshUsageCache[meshGuid] = list;
            }

            if (!list.Any(x => x.WorldId == worldId && x.EntityId == entityId))
            {
                list.Add((worldId, entityId));
            }
        }

        private void RemoveFromMeshCache(uint worldId, uint entityId, string meshGuid)
        {
            if (string.IsNullOrEmpty(meshGuid))
                return;

            if (_meshUsageCache.TryGetValue(meshGuid, out var list))
            {
                list.RemoveAll(tuple => tuple.WorldId == worldId && tuple.EntityId == entityId);

                if (list.Count == 0)
                {
                    _meshUsageCache.Remove(meshGuid);
                }
            }
        }

        private string GetMeshGuidFromComponent(IComponent component)
        {
            var type = component.GetType();
            var field = type.GetField("MeshGUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(component) as string;
            }

            return null;
        }

        private void HandleComponentAdded(uint worldId, uint entityId, IComponent component)
        {
            if (component is MeshComponent)
            {
                var meshGuid = GetMeshGuidFromComponent(component);
                if (!string.IsNullOrEmpty(meshGuid))
                {
                    AddToMeshCache(worldId, entityId, meshGuid);
                }
            }
        }

        private void HandleComponentRemoved(uint worldId, uint entityId, IComponent component)
        {
            if (component is MeshComponent)
            {
                var meshGuid = GetMeshGuidFromComponent(component);
                if (!string.IsNullOrEmpty(meshGuid))
                {
                    RemoveFromMeshCache(worldId, entityId, meshGuid);
                }
            }
        }

        private void HandleComponentChanged(uint worldId, uint entityId, IComponent component, bool ignoreSceneView)
        {
            if (component is MeshComponent)
            {
                string oldGuid = null;
                foreach (var kv in _meshUsageCache)
                {
                    if (kv.Value.Any(x => x.WorldId == worldId && x.EntityId == entityId))
                    {
                        oldGuid = kv.Key;
                        break;
                    }
                }

                var newGuid = GetMeshGuidFromComponent(component);

                if (oldGuid != newGuid)
                {
                    if (oldGuid != null)
                    {
                        RemoveFromMeshCache(worldId, entityId, oldGuid);
                    }

                    if (newGuid != null)
                    {
                        AddToMeshCache(worldId, entityId, newGuid);
                    }
                }
            }
        }

        public override void HandleDependencyChanged(string assetPath, string changedDependencyPath, FileMetadata dependencyMeta)
        {
            //if (_meshUsageCache.TryGetValue(dependencyMeta.Guid, out var affectedComponents))
            //{
            //    foreach (var (worldId, entityId) in affectedComponents)
            //    {
            //        try
            //        {
            //            if (SceneManager.EntityCompProvider.HasComponent<MeshComponent>(entityId))
            //            {
            //                ref MeshComponent meshComponent = ref SceneManager.EntityCompProvider.GetComponent<MeshComponent>(entityId);
            //                _sceneManager.ComponentChange(entityId, meshComponent, false);
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            DebLogger.Error($"Ошибка при обновлении MeshComponent (Entity: {entityId}): {ex.Message}");
            //        }
            //    }
            //}
        }

        public override void HandleDependencyDeleted(string assetPath, string deletedDependencyGuid, FileMetadata dependencyMeta)
        {
            if (_meshUsageCache.TryGetValue(dependencyMeta.Guid, out var affectedComponents))
            {
                foreach (var (worldId, entityId) in affectedComponents)
                {
                    try
                    {
                        if (SceneManager.EntityCompProvider.HasComponent<MeshComponent>(entityId))
                        {
                            ref MeshComponent meshComponent = ref SceneManager.EntityCompProvider.GetComponent<MeshComponent>(entityId);

                            TypedReference tr = __makeref(meshComponent);

                            var type = meshComponent.GetType();
                            var guidField = type.GetField("MeshGUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (guidField != null)
                            {
                                guidField.SetValueDirect(tr, string.Empty);
                            }

                            var indexField = type.GetField("MeshInternalIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (indexField != null)
                            {
                                indexField.SetValueDirect(tr, string.Empty);
                            }

                            meshComponent.Mesh = null;
                            _sceneManager.ComponentChange(entityId, meshComponent, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка при обновлении MeshComponent (Entity: {entityId}): {ex.Message}");
                    }
                }

                _meshUsageCache.Remove(dependencyMeta.Guid);
            }
        }

        public override void HandleDependencyAdded(string assetPath, string addedDependencyPath, FileMetadata dependencyMeta)
        { }

        public override void HandleDependencyRemoved(string assetPath, string removedDependencyPath, FileMetadata dependencyMeta)
        { }
    }
}
