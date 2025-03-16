using System.Collections.Generic;
using AtomEngine;
using System;

namespace Editor
{
    public static class HierarchyComponentRouter
    {
        internal static void EntityReordered(object? sender, EntityReorderEventArgs e, SceneManager sceneManager)
        {
            bool hasEntityHierarchy = SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(e.Entity.Id);
            if (!hasEntityHierarchy)
            {
                if (e.NewParentId.HasValue)
                {
                    sceneManager.AddComponent(e.Entity.Id, typeof(HierarchyComponent));
                    ref HierarchyComponent hierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(e.Entity.Id);
                    hierarchy.Parent = uint.MaxValue;
                }
                else
                {
                    return;
                }
            }

            ref HierarchyComponent entityHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(e.Entity.Id);

            uint oldLevel = entityHierarchy.Level;

            if (e.OldParentId.HasValue)
            {
                bool hasOldParentHierarchy = SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(e.OldParentId.Value);
                if (hasOldParentHierarchy)
                {
                    ref HierarchyComponent oldParentHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(e.OldParentId.Value);

                    if (oldParentHierarchy.Children != null && oldParentHierarchy.Children.Contains(e.Entity.Id))
                    {
                        oldParentHierarchy.Children.Remove(e.Entity.Id);
                        UpdateLocalIndicesForChildren(e.OldParentId.Value, e.OldLocalIndex);
                        if ((oldParentHierarchy.Children == null || oldParentHierarchy.Children.Count == 0) &&
                            oldParentHierarchy.Parent == uint.MaxValue)
                        {
                            sceneManager.RemoveComponent(e.OldParentId.Value, typeof(HierarchyComponent));
                        }
                    }
                }

                if (!e.NewParentId.HasValue)
                {
                    if (entityHierarchy.Children == null || entityHierarchy.Children.Count == 0)
                    {
                        sceneManager.RemoveComponent(e.Entity.Id, typeof(HierarchyComponent));
                        DebLogger.Debug(e);
                        return;
                    }
                    else
                    {
                        entityHierarchy.Parent = uint.MaxValue;
                    }
                }
            }

            if (e.NewParentId.HasValue)
            {
                bool hasNewParentHierarchy = SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(e.NewParentId.Value);
                if (!hasNewParentHierarchy)
                {
                    sceneManager.AddComponent(e.NewParentId.Value, typeof(HierarchyComponent));
                    ref HierarchyComponent newParentComponent = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(e.NewParentId.Value);
                    newParentComponent.Parent = uint.MaxValue;
                    newParentComponent.Children = new List<uint>();
                }

                ref HierarchyComponent newParentHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(e.NewParentId.Value);

                if (newParentHierarchy.Children == null)
                    newParentHierarchy.Children = new List<uint>();

                if (!newParentHierarchy.Children.Contains(e.Entity.Id))
                {
                    if (e.NewLocalIndex >= 0 && e.NewLocalIndex <= newParentHierarchy.Children.Count)
                    {
                        UpdateLocalIndicesForInsert(e.NewParentId.Value, e.NewLocalIndex);
                        newParentHierarchy.Children.Insert(e.NewLocalIndex, e.Entity.Id);
                    }
                    else
                    {
                        newParentHierarchy.Children.Add(e.Entity.Id);
                        entityHierarchy.LocalIndex = (uint)(newParentHierarchy.Children.Count - 1);
                    }
                }

                entityHierarchy.Parent = e.NewParentId.Value;
                entityHierarchy.LocalIndex = (uint)e.NewLocalIndex;
            }

            uint newLevel = e.Entity.Level > 0 ? (uint)e.Entity.Level : 0;
            entityHierarchy.Level = newLevel;

            if (entityHierarchy.Children != null && entityHierarchy.Children.Count > 0)
            {
                int levelDifference = (int)newLevel - (int)oldLevel;

                if (levelDifference != 0)
                {
                    UpdateChildrenLevels(e.Entity.Id, levelDifference);
                }
            }
        }

        private static void UpdateChildrenLevels(uint entityId, int levelDifference)
        {
            if (!SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(entityId))
                return;

            ref HierarchyComponent entityHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(entityId);
            if (entityHierarchy.Children == null || entityHierarchy.Children.Count == 0)
                return;

            foreach (uint childId in entityHierarchy.Children)
            {
                if (!SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(childId))
                    continue;

                ref HierarchyComponent childHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(childId);

                if (levelDifference > 0 || childHierarchy.Level >= (uint)Math.Abs(levelDifference))
                {
                    childHierarchy.Level = (uint)((int)childHierarchy.Level + levelDifference);
                    UpdateChildrenLevels(childId, levelDifference);
                }
            }
        }

        private static void UpdateLocalIndicesForChildren(uint parentId, int removedLocalIndex)
        {
            if (!SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(parentId))
                return;

            ref HierarchyComponent parentHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(parentId);

            if (parentHierarchy.Children == null || parentHierarchy.Children.Count == 0)
                return;

            for (int i = 0; i < parentHierarchy.Children.Count; i++)
            {
                uint childId = parentHierarchy.Children[i];
                if (SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(childId))
                {
                    ref HierarchyComponent childHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(childId);
                    if (childHierarchy.LocalIndex > removedLocalIndex)
                    {
                        childHierarchy.LocalIndex--;
                    }
                    childHierarchy.LocalIndex = (uint)i;
                }
            }
        }

        private static void UpdateLocalIndicesForInsert(uint parentId, int insertLocalIndex)
        {
            if (!SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(parentId))
                return;

            ref HierarchyComponent parentHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(parentId);

            if (parentHierarchy.Children == null)
                return;

            for (int i = 0; i < parentHierarchy.Children.Count; i++)
            {
                uint childId = parentHierarchy.Children[i];
                if (SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(childId))
                {
                    ref HierarchyComponent childHierarchy = ref SceneManager.EntityCompProvider.GetComponent<HierarchyComponent>(childId);

                    if (childHierarchy.LocalIndex >= insertLocalIndex)
                    {
                        childHierarchy.LocalIndex++;
                    }
                }
            }
        }

    }
}
