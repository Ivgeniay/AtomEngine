using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;
using System;
using Key = Avalonia.Input.Key;
using System.Collections.Generic;

namespace Editor
{
    internal class EntityHierarchyOperations
    {
        private readonly HierarchyController _controller;

        public EntityHierarchyOperations(HierarchyController controller)
        {
            _controller = controller;
        }

        public EntityHierarchyItem CreateHierarchyEntity(EntityData entityData)
        {
            var entityItem = new EntityHierarchyItem(entityData.Id, entityData.Version, entityData.Name);

            _controller.Entities.Add(entityItem);
            _controller.EntitiesList.SelectedItem = entityItem;

            return entityItem;
        }

        public void StartRenaming(EntityHierarchyItem entity)
        {
            var textBox = new TextBox
            {
                Text = entity.Name,
                Width = 200,
                SelectionStart = 0,
                SelectionEnd = entity.Name.Length,
                Classes = { "renameTextBox" }
            };

            var popup = new Popup
            {
                Child = textBox,
                Placement = PlacementMode.Pointer,
                IsOpen = true
            };

            _controller.Children.Add(popup);

            textBox.Focus();

            textBox.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        entity.Name = textBox.Text;
                        _controller.OnEntityRenamed(entity);

                        //if (_controller.EntitiesList.ItemsSource is ObservableCollection<EntityHierarchyItem> collection)
                        //if (_controller.EntitiesList.ItemsSource is List<EntityHierarchyItem> collection)
                        //{
                        //    //var collection = _controller.EntitiesList.ItemsSource;
                        //    //Type t = collection.GetType();
                        //    var index = collection.IndexOf(entity);
                        //    if (index != -1)
                        //    {
                        //        collection.RemoveAt(index);
                        //        collection.Insert(index, entity);
                        //    }
                        //}
                    }
                    popup.IsOpen = false;
                    _controller.Children.Remove(popup);
                }
                else if (e.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    _controller.Children.Remove(popup);
                }
            };

            textBox.LostFocus += (s, e) => {
                popup.IsOpen = false;
                _controller.Children.Remove(popup);
            };
        }

        public void DuplicateEntity(EntityHierarchyItem entity)
        {
            _controller.OnEntityDuplicated(entity);
        }

        public void DeleteEntity(EntityHierarchyItem entity)
        {
            _controller.Entities.Remove(entity);
            _controller.OnEntityDeleted(entity);
        }

        public string GetUniqueName(string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (_controller.Entities.Any(e => e.Name == name))
            {
                name = $"{baseName} ({counter})";
                counter++;
            }
            return name;
        }

        public void OnEntitiesListDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (_controller.EntitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                StartRenaming(selectedEntity);
            }
        }

        public void SetParent(uint childId, uint? parentId, out EntityReorderEventArgs eventArgs)
        {
            eventArgs = null;
            var dataManager = new HierarchyDataManager(_controller);

            int childIndex = FindIndex(_controller.Entities, e => e.Id == childId);
            if (childIndex < 0) return;

            var child = _controller.Entities[childIndex];

            if (child.ParentId == parentId) return;

            int oldIndex = childIndex;
            uint? oldParentId = child.ParentId;
            int oldLocalIndex = CalculateLocalIndex(child);

            var tree = dataManager.BuildTreeFromEntity(childId);
            if (tree == null) return;

            dataManager.RemoveEntityTree(childId);

            if (oldParentId != null)
            {
                int oldParentIndex = FindIndex(_controller.Entities, e => e.Id == oldParentId);
                if (oldParentIndex >= 0)
                {
                    var oldParent = _controller.Entities[oldParentIndex];
                    oldParent.Children.Remove(childId);
                    _controller.Entities[oldParentIndex] = oldParent;
                }
            }

            tree.Root.ParentId = parentId;
            tree.Root.Level = parentId == null ? 0 : tree.Root.Level;

            int targetIndex = -1;

            if (parentId == null)
            {
                tree.Root.ParentId = null;
                tree.Root.Level = 0;

                targetIndex = oldIndex;
            }
            else
            {
                int parentIndex = FindIndex(_controller.Entities, e => e.Id == parentId);
                if (parentIndex >= 0)
                {
                    var parent = _controller.Entities[parentIndex];

                    if (!IsValidParent(childId, parentId))
                    {
                        dataManager.AddEntityTree(tree, oldIndex);
                        return;
                    }

                    tree.Root.Level = parent.Level + 1;

                    parent.Children.Add(childId);
                    _controller.Entities[parentIndex] = parent;

                    int lastChildIndex = dataManager.FindLastDescendantIndex(parentId.Value);
                    targetIndex = lastChildIndex + 1;
                }
            }

            dataManager.AddEntityTree(tree, targetIndex);

            int newIndex = FindIndex(_controller.Entities, e => e.Id == childId);
            if (newIndex < 0) return;

            var updatedChild = _controller.Entities[newIndex];
            int newLocalIndex = CalculateLocalIndex(updatedChild);

            eventArgs = new EntityReorderEventArgs(
                updatedChild,
                oldIndex,
                newIndex,
                oldLocalIndex,
                newLocalIndex,
                updatedChild.ParentId,
                oldParentId);

            _controller.RefreshHierarchyVisibility();
        }

        private bool IsValidParent(uint childId, uint? parentId)
        {
            if (parentId == null) return true;
            if (childId == parentId) return false;

            uint? currentParent = parentId;
            while (currentParent != null)
            {
                if (currentParent == childId) return false;

                int parentIndex = FindIndex(_controller.Entities, e => e.Id == currentParent);
                if (parentIndex < 0) break;

                currentParent = _controller.Entities[parentIndex].ParentId;
            }

            return true;
        }

        private int CalculateLocalIndex(EntityHierarchyItem item)
        {
            if (item.ParentId != null)
            {
                var siblings = _controller.Entities.Where(e => e.ParentId == item.ParentId).ToList();
                return siblings.FindIndex(e => e.Id == item.Id);
            }
            else
            {
                var rootItems = _controller.Entities.Where(e => e.ParentId == null).ToList();
                return rootItems.FindIndex(e => e.Id == item.Id);
            }
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