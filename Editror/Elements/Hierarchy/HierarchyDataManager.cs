using System.Collections.ObjectModel;
using System.Linq;
using AtomEngine;
using System;
using System.Collections.Generic;

namespace Editor
{
    internal class HierarchyDataManager
    {
        private readonly HierarchyController _controller;

        public HierarchyDataManager(HierarchyController controller)
        {
            _controller = controller;
        }

        public void BuildHierarchyFromComponents()
        {
            if (_controller.CurrentScene == null || _controller.CurrentScene.CurrentWorldData == null)
                return;

            _controller.ClearEntities();

            var allEntities = _controller.CurrentScene.CurrentWorldData.Entities.ToList();
            Dictionary<uint, EntityHierarchyItem> idToItem = new Dictionary<uint, EntityHierarchyItem>();
            
            foreach (var entityData in allEntities)
            {
                var hierarchyItem = new EntityHierarchyItem(entityData.Id, entityData.Version, entityData.Name);
                hierarchyItem.Children = new List<uint>();

                if (SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(entityData.Id))
                {
                    ref var hierarchyComp = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(entityData.Id);
                    if (hierarchyComp.Parent != uint.MaxValue)
                    {
                        hierarchyItem.ParentId = hierarchyComp.Parent;
                    }
                    hierarchyItem.Level = (int)hierarchyComp.Level;
                    if (hierarchyComp.Children != null && hierarchyComp.Children.Count > 0)
                    {
                        hierarchyItem.Children = hierarchyComp.Children.ToList();
                    }
                }
                idToItem[entityData.Id] = hierarchyItem;
            }

             
            List<EntityHierarchyItem> flattenedHierarchy = new List<EntityHierarchyItem>();
            var rootEntities = idToItem.Values
                .Where(item => item.ParentId == null || item.ParentId == uint.MaxValue)
                .OrderBy<EntityHierarchyItem, uint>(item =>
                {
                    if (SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(item.Id))
                    {
                        ref var comp = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(item.Id);
                        return comp.LocalIndex;
                    }
                    return 0;
                })
                .ToList();

             
            foreach (var rootEntity in rootEntities)
            {
                flattenedHierarchy.Add(rootEntity);
                AddChildrenRecursively(rootEntity.Id, idToItem, flattenedHierarchy);
            }

             
            foreach (var item in flattenedHierarchy)
            {
                _controller.Entities.Add(item);
            }

            RefreshHierarchyVisibility();
        }

        private void AddChildrenRecursively(uint parentId, Dictionary<uint, EntityHierarchyItem> idToItem, List<EntityHierarchyItem> result)
        {
            if (!idToItem.TryGetValue(parentId, out var parentItem))
                return;

            if (parentItem.Children == null || parentItem.Children.Count == 0)
                return;

            var childrenWithLocalIndices = parentItem.Children
                .Where(childId => idToItem.ContainsKey(childId))
                .Select(childId =>
                {
                    uint localIndex = 0;
                    if (SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(childId))
                    {
                        ref var comp = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(childId);
                        localIndex = comp.LocalIndex;
                    }
                    return new { childId, localIndex };
                })
                .OrderBy(x => x.localIndex)
                .Select(x => x.childId)
                .ToList();

            foreach (var childId in childrenWithLocalIndices)
            {
                if (idToItem.TryGetValue(childId, out var childItem))
                {
                    childItem.ParentId = parentId;
                    childItem.Level = parentItem.Level + 1;

                    result.Add(childItem);
                    AddChildrenRecursively(childId, idToItem, result);
                }
            }
        }

        public void RefreshHierarchyVisibility()
        {
            var entitiesToProcess = _controller.Entities.ToList();
            var updates = new List<(int index, EntityHierarchyItem entity)>();

            foreach (var entity in entitiesToProcess)
            {
                bool isVisible = true;
                if (entity.ParentId != null)
                {
                    var parent = entitiesToProcess.FirstOrDefault(e => e.Id == entity.ParentId);
                    uint? currentParentId = entity.ParentId;
                    while (currentParentId != null)
                    {
                        var currentParent = entitiesToProcess.FirstOrDefault(e => e.Id == currentParentId);
                        if (currentParent != EntityHierarchyItem.Null && !currentParent.IsExpanded)
                        {
                            isVisible = false;
                            break;
                        }
                        currentParentId = currentParent.ParentId;
                    }
                }

                var updatedEntity = entity;
                updatedEntity.IsVisible = isVisible;

                 
                bool hasChildren = entitiesToProcess.Any(e => e.ParentId == entity.Id);
                if (updatedEntity.Children.Count > 0 != hasChildren)
                {
                    var childrenIds = entitiesToProcess
                        .Where(e => e.ParentId == entity.Id)
                        .Select(e => e.Id)
                        .ToList();
                    updatedEntity.Children = childrenIds;
                }

                int index = FindIndex(_controller.Entities, e => e.Id == entity.Id);
                if (index >= 0)
                {
                    updates.Add((index, updatedEntity));
                }
            }

             
            foreach (var (index, updatedEntity) in updates)
            {
                if (index < _controller.Entities.Count)
                {
                    _controller.Entities[index] = updatedEntity;
                }
            }

             
            var visibleEntities = _controller.Entities.Where(e => e.IsVisible).ToList();
            _controller.EntitiesList.ItemsSource = null;
            _controller.EntitiesList.ItemsSource = visibleEntities;
        }

        public List<EntityHierarchyItem> GatherDescendants(uint entityId)
        {
            var result = new List<EntityHierarchyItem>();
            var queue = new Queue<uint>();
            queue.Enqueue(entityId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                if (currentId != entityId)
                {
                    var entity = _controller.Entities.FirstOrDefault(e => e.Id == currentId);
                    if (entity != EntityHierarchyItem.Null)
                        result.Add(entity);
                }

                var directChildren = _controller.Entities
                    .Where(e => e.ParentId == currentId)
                    .OrderBy(e => FindIndex(_controller.Entities, x => x.Id == e.Id))
                    .ToList();

                foreach (var child in directChildren)
                {
                    queue.Enqueue(child.Id);
                }
            }

            return result;
        }

        public int FindLastDescendantIndex(uint entityId)
        {
            int lastIndex = FindIndex(_controller.Entities, e => e.Id == entityId);

            foreach (var entity in _controller.Entities)
            {
                if (entity.ParentId == entityId)
                {
                    int descendantLastIndex = FindLastDescendantIndex(entity.Id);
                    if (descendantLastIndex > lastIndex)
                    {
                        lastIndex = descendantLastIndex;
                    }
                }
            }

            return lastIndex;
        }

        public int FindLastVisibleDescendantIndex(uint entityId, List<EntityHierarchyItem> visibleItems)
        {
            int lastIndex = visibleItems.FindIndex(e => e.Id == entityId);

            var directChildren = visibleItems
                .Where(e => e.ParentId == entityId)
                .ToList();

            foreach (var child in directChildren)
            {
                if (child.IsExpanded && child.Children.Count > 0)
                {
                    int descendantIndex = FindLastVisibleDescendantIndex(child.Id, visibleItems);
                    if (descendantIndex > lastIndex)
                    {
                        lastIndex = descendantIndex;
                    }
                }
                else
                {
                    int childIndex = visibleItems.FindIndex(e => e.Id == child.Id);
                    if (childIndex > lastIndex)
                    {
                        lastIndex = childIndex;
                    }
                }
            }

            return lastIndex;
        }

        public EntityHierarchyItemTree BuildTreeFromEntity(uint entityId)
        {
            int index = FindIndex(_controller.Entities, e => e.Id == entityId);
            if (index < 0) return null;

            var entity = _controller.Entities[index];
            var tree = new EntityHierarchyItemTree(entity);

            var childrenIds = entity.Children.ToList();

            foreach (var childId in childrenIds)
            {
                var childTree = BuildTreeFromEntity(childId);
                if (childTree != null)
                {
                    tree.AddChild(childTree);
                }
            }

            return tree;
        }

        public void RemoveEntityTree(uint rootId)
        {
            var descendants = GatherDescendants(rootId);
            var allIds = new List<uint> { rootId };
            allIds.AddRange(descendants.Select(d => d.Id));

            for (int i = allIds.Count - 1; i >= 0; i--)
            {
                int index = FindIndex(_controller.Entities, e => e.Id == allIds[i]);
                if (index >= 0)
                {
                    _controller.Entities.RemoveAt(index);
                }
            }

            RefreshHierarchyVisibility();
        }

        public void AddEntityTree(EntityHierarchyItemTree tree, int targetIndex = -1)
        {
            var items = tree.FlattenTree();

            var root = tree.Root;
            if (root.ParentId == null)
            {
                root.Level = 0;
            }

            if (targetIndex >= 0 && targetIndex <= _controller.Entities.Count)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    _controller.Entities.Insert(targetIndex + i, items[i]);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    _controller.Entities.Add(item);
                }
            }

            _controller.EntitiesList.SelectedItem = tree.Root;

            RefreshHierarchyVisibility();
        }

        private int FindIndex(ObservableCollection<EntityHierarchyItem> collection, Func<EntityHierarchyItem, bool> predicate)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                    return i;
            }
            return -1;
        }
    }

}