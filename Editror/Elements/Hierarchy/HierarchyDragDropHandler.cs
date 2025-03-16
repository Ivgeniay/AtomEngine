using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Data;
using System.Linq;
using Avalonia;
using System;
using Avalonia.VisualTree;

namespace Editor
{
    internal class HierarchyDragDropHandler
    {
        private readonly HierarchyController _controller;
        private readonly ListBox _entitiesList;
        private readonly Canvas _indicatorCanvas;
        private readonly Border _dropIndicator;

        private EntityHierarchyItem? _draggedItem;
        private int _draggedIndex = -1;
        private bool _isDragging = false;
        private int _dropTargetIndex = -1;
        private uint? _targetParentId;
        private bool _asChild;

        public event EventHandler<EntityReorderEventArgs> EntityReordered;

        public HierarchyDragDropHandler(HierarchyController controller, ListBox entitiesList, Canvas indicatorCanvas, Border dropIndicator)
        {
            _controller = controller;
            _entitiesList = entitiesList;
            _indicatorCanvas = indicatorCanvas;
            _dropIndicator = dropIndicator;
        }

        public void OnEntityPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(_entitiesList);
                var visual = e.Source as Visual;

                if (visual?.DataContext is EntityHierarchyItem item)
                {
                    var listItems = _entitiesList.ItemsSource.Cast<EntityHierarchyItem>().ToList();
                    _draggedIndex = listItems.FindIndex(entity => entity.Id == item.Id);

                    if (_draggedIndex != -1)
                    {
                        _draggedItem = item;
                        _isDragging = false;
                        _controller.SelectedFile = item;
                        _controller.OnEntitySelected(item);
                    }
                }
            }
        }

        public void OnEntityPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedIndex != -1 && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _controller.Cursor = new Cursor(StandardCursorType.DragMove);
                }

                var point = e.GetPosition(_entitiesList);
                var scrollViewer = _entitiesList.FindDescendantOfType<ScrollViewer>();

                if (scrollViewer != null)
                {
                    if (point.Y < 20 && scrollViewer.Offset.Y > 0)
                    {
                        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, scrollViewer.Offset.Y - 5));
                    }
                    else if (point.Y > _entitiesList.Bounds.Height - 20 && scrollViewer.Offset.Y < scrollViewer.Extent.Height - scrollViewer.Viewport.Height)
                    {
                        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Min(scrollViewer.Extent.Height - scrollViewer.Viewport.Height, scrollViewer.Offset.Y + 5));
                    }
                }

                CalculateDropPosition(point);

                e.Handled = true;
            }
        }

        public void OnEntityPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging && _draggedItem != null && _dropTargetIndex != -1)
            {
                if (_asChild && _targetParentId != null)
                {
                    _controller.SetParent(_draggedItem.Value.Id, _targetParentId);
                }
                else if (_dropTargetIndex != _draggedIndex && (_dropTargetIndex != _draggedIndex + 1 || _draggedItem.Value.ParentId != _targetParentId))
                {
                    ReorderEntity(_draggedItem.Value, _dropTargetIndex, _targetParentId);
                }
                _controller.SelectedFile = _draggedItem.Value;
            }
            else if (_draggedItem != null && !_isDragging)
            {
                var visual = e.Source as Visual;
                if (visual != null)
                {
                    if (visual.DataContext is EntityHierarchyItem clickedItem)
                    {
                        if (clickedItem != null && clickedItem == _controller.SelectedFile)
                        {
                            _controller.SelectedFile = EntityHierarchyItem.Null;
                            _controller.OnEntitySelected(clickedItem);
                        }
                    }
                }
            }

            _draggedItem = null;
            _draggedIndex = -1;
            _isDragging = false;
            _dropTargetIndex = -1;
            _targetParentId = null;
            _asChild = false;
            _dropIndicator.IsVisible = false;
            _controller.Cursor = Cursor.Default;
        }

        public void OnEntityListItemPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetCurrentPoint(null);

            if (point.Properties.IsRightButtonPressed || point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                var element = e.Source as Visual;
                if (element != null)
                {
                    while (element != null && !(element.DataContext is EntityHierarchyItem))
                    {
                        element = element.GetVisualParent();
                    }

                    if (element != null && element.DataContext is EntityHierarchyItem entityItem)
                    {
                        _entitiesList.SelectedItem = entityItem;
                        _controller.OnEntitySelected(entityItem);

                        //_entityContextMenu.Open(_controller);
                        e.Handled = true;
                    }
                }
            }
        }

        private void CalculateDropPosition(Point point)
        {
            if (_entitiesList.ItemsSource == null) return;

            var listItems = _entitiesList.ItemsSource.Cast<EntityHierarchyItem>().ToList();
            var newDropTargetIndex = -1;
            uint? newParentId = null;
            bool asChild = false;

            bool draggingToRoot = point.X < 20;

            if (draggingToRoot)
            {
                newParentId = null;
                asChild = false;

                for (int i = 0; i < listItems.Count; i++)
                {
                    var container = _entitiesList.ContainerFromIndex(i) as Control;
                    if (container != null)
                    {
                        var containerTopInList = container.TranslatePoint(new Point(0, 0), _entitiesList)?.Y ?? 0;
                        var containerBounds = container.Bounds;

                        if (point.Y < containerTopInList + (containerBounds.Height / 2))
                        {
                            newDropTargetIndex = i;
                            break;
                        }
                        else if (i == listItems.Count - 1 &&
                             point.Y >= containerTopInList + (containerBounds.Height / 2))
                        {
                            newDropTargetIndex = i + 1;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < listItems.Count; i++)
                {
                    var container = _entitiesList.ContainerFromIndex(i) as Control;
                    if (container != null)
                    {
                        var containerBounds = container.Bounds;
                        var containerTopInList = container.TranslatePoint(new Point(0, 0), _entitiesList)?.Y ?? 0;

                        if (point.Y >= containerTopInList && point.Y < containerTopInList + containerBounds.Height)
                        {
                            var horizontalPos = point.X;
                            var currentItem = listItems[i];
                            var itemIndent = currentItem.Level * 10 + 20;

                            if (horizontalPos > itemIndent + 10 && horizontalPos < itemIndent + 30)
                            {
                                newParentId = currentItem.Id;
                                asChild = true;
                                newDropTargetIndex = i;
                            }
                            else
                            {
                                int targetLevel = (int)(horizontalPos / 10);
                                targetLevel = Math.Min(targetLevel, currentItem.Level);

                                if (targetLevel <= currentItem.Level)
                                {
                                    uint? ancestorId = currentItem.ParentId;
                                    int currentLevel = currentItem.Level - 1;

                                    while (ancestorId != null && currentLevel > targetLevel)
                                    {
                                        var ancestor = listItems.FirstOrDefault(a => a.Id == ancestorId);
                                        if (ancestor != null && ancestor != EntityHierarchyItem.Null)
                                        {
                                            ancestorId = ancestor.ParentId;
                                            currentLevel--;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    newParentId = ancestorId;
                                }

                                if (point.Y < containerTopInList + (containerBounds.Height / 2))
                                {
                                    newDropTargetIndex = i;
                                }
                                else
                                {
                                    newDropTargetIndex = i + 1;

                                    if (currentItem.IsExpanded && currentItem.Children.Count > 0)
                                    {
                                        var dataManager = new HierarchyDataManager(_controller);
                                        var lastDescendantIndex = dataManager.FindLastVisibleDescendantIndex(currentItem.Id, listItems);
                                        if (lastDescendantIndex > i)
                                        {
                                            newDropTargetIndex = lastDescendantIndex + 1;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }

            if (newDropTargetIndex == -1 && listItems.Count > 0)
            {
                var lastContainer = _entitiesList.ContainerFromIndex(listItems.Count - 1) as Control;
                if (lastContainer != null)
                {
                    var lastContainerBottom = lastContainer.TranslatePoint(new Point(0, lastContainer.Bounds.Height), _entitiesList)?.Y ?? 0;
                    if (point.Y >= lastContainerBottom)
                    {
                        newDropTargetIndex = listItems.Count;
                        newParentId = null;
                        asChild = false;
                    }
                }
            }

            if (newDropTargetIndex == _draggedIndex || (newDropTargetIndex == _draggedIndex + 1 && newParentId == _draggedItem?.ParentId))
            {
                _dropIndicator.IsVisible = false;
                _dropTargetIndex = -1;
                _targetParentId = null;
                _asChild = false;
                return;
            }

            if (_draggedItem != null && newParentId != null && (_draggedItem.Value.Id == newParentId || IsChildOf(_draggedItem.Value.Id, newParentId.Value)))
            {
                _dropIndicator.IsVisible = false;
                _dropTargetIndex = -1;
                _targetParentId = null;
                _asChild = false;
                return;
            }

            _dropTargetIndex = newDropTargetIndex;
            _targetParentId = newParentId;
            _asChild = asChild;

            if (_dropTargetIndex >= 0)
            {
                _dropIndicator.IsVisible = true;

                double indicatorY;
                double indicatorX = 0;
                double indicatorWidth;

                if (_asChild)
                {
                    var container = _entitiesList.ContainerFromIndex(_dropTargetIndex) as Control;
                    if (container != null)
                    {
                        var containerBounds = container.Bounds;
                        indicatorY = container.TranslatePoint(new Point(0, containerBounds.Height), _entitiesList)?.Y ?? 0;
                        var targetItem = listItems[_dropTargetIndex];
                        indicatorX = targetItem.Level * 10 + 20;
                        indicatorWidth = _entitiesList.Bounds.Width - indicatorX;
                    }
                    else
                    {
                        _dropIndicator.IsVisible = false;
                        return;
                    }
                }
                else if (_dropTargetIndex >= listItems.Count)
                {
                    var lastContainer = _entitiesList.ContainerFromIndex(listItems.Count - 1) as Control;
                    if (lastContainer != null)
                    {
                        var lastContainerBottom = lastContainer.TranslatePoint(new Point(0, lastContainer.Bounds.Height), _entitiesList)?.Y ?? 0;
                        indicatorY = lastContainerBottom;
                        indicatorWidth = _entitiesList.Bounds.Width;
                    }
                    else
                    {
                        _dropIndicator.IsVisible = false;
                        return;
                    }
                }
                else
                {
                    var container = _entitiesList.ContainerFromIndex(_dropTargetIndex) as Control;
                    if (container != null)
                    {
                        indicatorY = container.TranslatePoint(new Point(0, 0), _entitiesList)?.Y ?? 0;

                        if (newParentId != null)
                        {
                            var parentItem = _controller.Entities.FirstOrDefault(e => e.Id == newParentId);
                            if (parentItem != EntityHierarchyItem.Null)
                            {
                                indicatorX = parentItem.Level * 20 + 40;
                                indicatorWidth = _entitiesList.Bounds.Width - indicatorX;
                            }
                            else
                            {
                                indicatorWidth = _entitiesList.Bounds.Width;
                            }
                        }
                        else
                        {
                            indicatorWidth = _entitiesList.Bounds.Width;
                        }
                    }
                    else
                    {
                        _dropIndicator.IsVisible = false;
                        return;
                    }
                }

                var listBoxPoint = _entitiesList.TranslatePoint(new Point(indicatorX, indicatorY), _indicatorCanvas) ?? new Point(0, 0);

                _dropIndicator.Width = indicatorWidth;
                Canvas.SetTop(_dropIndicator, listBoxPoint.Y - _dropIndicator.Height / 2);
                Canvas.SetLeft(_dropIndicator, listBoxPoint.X);
            }
            else
            {
                _dropIndicator.IsVisible = false;
            }
        }

        private bool IsChildOf(uint parentId, uint childId)
        {
            foreach (var entity in _controller.Entities)
            {
                if (entity.Id == childId && entity.ParentId == parentId)
                {
                    return true;
                }
                else if (entity.Id == childId && entity.ParentId != null)
                {
                    return IsChildOf(parentId, entity.ParentId.Value);
                }
            }
            return false;
        }

        private void ReorderEntity(EntityHierarchyItem item, int newIndex, uint? newParentId = null)
        {
            var dataManager = new HierarchyDataManager(_controller);
            int oldIndex = FindIndex(_controller.Entities, e => e.Id == item.Id && e.Version == item.Version);
            if (oldIndex < 0) return;

            uint? oldParentId = item.ParentId;
            int oldLocalIndex = CalculateLocalIndex(item);

            if (oldParentId != null && newParentId == null)
            {
                var updatedRoot = item;
                updatedRoot.ParentId = null;
                updatedRoot.Level = 0;

                _controller.Entities[oldIndex] = updatedRoot;
            }

            var tree = dataManager.BuildTreeFromEntity(item.Id);
            if (tree == null) return;

            dataManager.RemoveEntityTree(item.Id);

            if (newParentId != null && oldParentId != newParentId)
            {
                int parentIndex = FindIndex(_controller.Entities, e => e.Id == newParentId);
                if (parentIndex >= 0)
                {
                    var parent = _controller.Entities[parentIndex];
                    if (!IsValidParent(item.Id, newParentId))
                    {
                        dataManager.AddEntityTree(tree, oldIndex);
                        return;
                    }
                    tree.Root.ParentId = newParentId;
                    tree.Root.Level = parent.Level + 1;

                    int lastChildIndex = dataManager.FindLastDescendantIndex(newParentId.Value);
                    if (lastChildIndex < parentIndex) lastChildIndex = parentIndex;

                    dataManager.AddEntityTree(tree, lastChildIndex + 1);

                    var updatedParent = _controller.Entities[parentIndex];
                    updatedParent.Children.Add(item.Id);
                    _controller.Entities[parentIndex] = updatedParent;
                }
            }
            else
            {
                if (newParentId == null)
                {
                    tree.Root.ParentId = null;
                    tree.Root.Level = 0;
                }
                dataManager.AddEntityTree(tree, newIndex);
            }

            int newEntityIndex = FindIndex(_controller.Entities, e => e.Id == item.Id);
            if (newEntityIndex < 0) return;

            var updatedItem = _controller.Entities[newEntityIndex];
            int newLocalIndex = CalculateLocalIndex(updatedItem);

            var eventArgs = new EntityReorderEventArgs(
                updatedItem,
                oldIndex,
                newEntityIndex,
                oldLocalIndex,
                newLocalIndex,
                updatedItem.ParentId,
                oldParentId);

            EntityReordered?.Invoke(this, eventArgs);
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