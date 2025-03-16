using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Data;
using System.Linq;
using AtomEngine;
using Avalonia;
using System;
using Key = Avalonia.Input.Key;
using MouseButton = Avalonia.Input.MouseButton;
using System.Collections.Generic;
using Avalonia.VisualTree;
using Newtonsoft.Json;

namespace Editor
{
    internal class HierarchyController : Grid, IWindowed
    {
        public Action<object> OnClose { get; set; }

        private ObservableCollection<EntityHierarchyItem> _entities;
        private ContextMenu _backgroundContextMenu;
        private ContextMenu _entityContextMenu;
        private ProjectScene _currentScene;
        private ListBox _entitiesList;
        private Canvas _indicatorCanvas;
        private bool isOpen = false;
        
        public event EventHandler<String> EntityCreated;
        public event EventHandler<EntityHierarchyItem> EntitySelected;
        public event EventHandler<EntityHierarchyItem> EntityDuplicated;
        public event EventHandler<EntityHierarchyItem> EntityRenamed;
        public event EventHandler<EntityHierarchyItem> EntityDeleted;
        public event EventHandler<EntityReorderEventArgs> EntityReordered;

        private SceneManager _sceneManager;
        private EntityHierarchyItem _selectedFile = EntityHierarchyItem.Null;



        public HierarchyController()
        {
            _entities = new ObservableCollection<EntityHierarchyItem>();
            InitializeUI();

            EntityReordered += (s, e) =>
            {
                DebLogger.Debug(e);
            };

            _entityContextMenu = CreateEntityComtexMenu();
            _backgroundContextMenu = CreateBGContextMenus();

            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.OnSceneInitialize += UpdateHyerarchy;

            _sceneManager.OnEntityCreated += (worldId, entityId) =>
            {
                var entityData = _sceneManager.CurrentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityId);
                if (entityData != null)
                {
                    CreateHierarchyEntity(entityData);
                }
            };
            _sceneManager.OnEntityDuplicated += (worldId, entityIdFrom, entityIdTo) =>
            {
                var entityData = _sceneManager.CurrentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityIdTo);
                if (entityData != null)
                {
                    CreateHierarchyEntity(entityData);
                }
            };
            _sceneManager.OnWorldSelected += (worldId, worldName) =>
            {
                UpdateHyerarchy(_sceneManager.CurrentScene);
            };
        }

        private void InitializeUI()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var header = new Border
            {
                Classes = { "hierarchyHeader" },
            };
            Grid.SetRow(header, 0);

            _entitiesList = new ListBox
            {
                Classes = { "entityList" },
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0)
            };

            _entitiesList.ItemsSource = _entities;
            _entitiesList.AutoScrollToSelectedItem = true;
            _entitiesList.SelectionMode = SelectionMode.Multiple;
            _entitiesList.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            _entitiesList.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            _entitiesList.MaxHeight = 10000;
            _entitiesList.Margin = new Thickness(0);

            _entitiesList.SelectionChanged += SelectCallback;

            _entitiesList.AddHandler(InputElement.PointerReleasedEvent, OnEntityListItemPointerReleased, RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerPressedEvent, OnEntityPointerPressed, RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerMovedEvent, OnEntityPointerMoved, RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerReleasedEvent, OnEntityPointerReleased, RoutingStrategies.Tunnel);


            ScrollViewer.SetHorizontalScrollBarVisibility(_entitiesList, ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(_entitiesList, ScrollBarVisibility.Auto);


            _entitiesList.ItemTemplate = CreateEntityItemTemplate();

            Grid.SetRow(_entitiesList, 1);

            _dropIndicator = new Border
            {
                Height = 2,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DodgerBlue),
                IsVisible = false,
                ZIndex = 1000
            };

            _indicatorCanvas = new Canvas
            {
                Background = null,  
                ZIndex = 100        
            };
            Grid.SetRow(_indicatorCanvas, 1);
            _indicatorCanvas.Children.Add(_dropIndicator);

            Children.Add(header);
            Children.Add(_entitiesList);
            Children.Add(_indicatorCanvas);

            _entitiesList.ZIndex = 2;
            _indicatorCanvas.ZIndex = 3;

            PointerReleased += OnHierarchyPointerPressed;
        }

        private ContextMenu CreateBGContextMenus()
        {
            var _backgroundContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var createEntityItem = new MenuItem
            {
                Header = "Create Entity",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateNewEntity)
            };

            var separatorItem = new MenuItem
            {
                Header = "-",
                Classes = { "hierarchySeparator" }
            };

            var transform3dItem = new MenuItem
            {
                Header = "3D Object",
                Classes = { "hierarchyMenuItem" }
            };

            var cubeItem = new MenuItem
            {
                Header = "Cube",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateCube)
            };

            var sphereItem = new MenuItem
            {
                Header = "Sphere",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateSphere)
            };

            var capsuleItem = new MenuItem
            {
                Header = "Capsule",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateCapsule)
            };

            var cylinderItem = new MenuItem
            {
                Header = "Cylinder",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateCylinder)
            };

            var planeItem = new MenuItem
            {
                Header = "Plane",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreatePlane)
            };

            transform3dItem.Items.Add(cubeItem);
            transform3dItem.Items.Add(sphereItem);
            transform3dItem.Items.Add(capsuleItem);
            transform3dItem.Items.Add(cylinderItem);
            transform3dItem.Items.Add(planeItem);

            _backgroundContextMenu.Items.Add(createEntityItem);
            _backgroundContextMenu.Items.Add(separatorItem);
            _backgroundContextMenu.Items.Add(transform3dItem);

            return _backgroundContextMenu;
        }
        
        private ContextMenu CreateEntityComtexMenu()
        {
            var _entityContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var renameItem = new MenuItem
            {
                Header = "Rename",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(StartRenamingCommand)
            };

            var duplicateItem = new MenuItem
            {
                Header = "Duplicate",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(DuplicateEntityCommand)
            };

            var deleteItem = new MenuItem
            {
                Header = "Delete",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(DeleteEntityCommand)
            };

            var entitySeparator = new MenuItem
            {
                Header = "-",
                Classes = { "hierarchySeparator" }
            };

            var addComponentItem = new MenuItem
            {
                Header = "Add Component",
                Classes = { "hierarchyMenuItem" }
            };

            var physicsItem = new MenuItem
            {
                Header = "Physics",
                Classes = { "hierarchyMenuItem" }
            };
            var renderingItem = new MenuItem
            {
                Header = "Rendering",
                Classes = { "hierarchyMenuItem" }
            };

            addComponentItem.Items.Add(physicsItem);
            addComponentItem.Items.Add(renderingItem);

            _entityContextMenu.Items.Add(renameItem);
            _entityContextMenu.Items.Add(duplicateItem);
            _entityContextMenu.Items.Add(deleteItem);
            _entityContextMenu.Items.Add(entitySeparator);
            _entityContextMenu.Items.Add(addComponentItem);

            return _entityContextMenu;
        }

        private IDataTemplate CreateEntityItemTemplate()
        {
            return new FuncDataTemplate<EntityHierarchyItem>((entity, scope) =>
            {
                var grid = new Grid
                {
                    Classes = { "entityCell" }
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var indent = new Border
                {
                    Width = entity.Level * 20,
                    Background = null
                };
                Grid.SetColumn(indent, 0);

                var expandButton = new ToggleButton
                {
                    Classes = { "expandButton" },
                    IsChecked = entity.IsExpanded,
                    Content = entity.IsExpanded ? "▼" : "►",
                    Width = 10,  
                    Height = 10, 
                    IsVisible = entity.Children.Count > 0
                };

                expandButton.Click += (s, e) => {
                    if (s is ToggleButton button && button.DataContext is EntityHierarchyItem item)
                    {
                        var updatedItem = item;
                        updatedItem.IsExpanded = !item.IsExpanded;
                        button.Content = updatedItem.IsExpanded ? "▼" : "►";

                        int index = FindIndex(_entities, e => e.Id == item.Id);
                        if (index >= 0)
                        {
                            _entities[index] = updatedItem;
                            UpdateChildrenVisibility(updatedItem.Id, updatedItem.IsExpanded);
                        }

                        e.Handled = true;
                    }
                };
                Grid.SetColumn(expandButton, 1);

                var iconAndNameContainer = new StackPanel
                {
                    Classes = { "entityCell" },
                    Orientation = Avalonia.Layout.Orientation.Horizontal
                };

                var entityIcon = new TextBlock
                {
                    Text = "⬚",
                    Classes = { "entityIcon" },
                };

                var entityName = new TextBlock
                {
                    Classes = { "entityName" }
                };
                entityName.Bind(TextBlock.TextProperty, new Binding("Name"));

                iconAndNameContainer.Children.Add(entityIcon);
                iconAndNameContainer.Children.Add(entityName);
                Grid.SetColumn(iconAndNameContainer, 2);

                grid.Children.Add(indent);
                grid.Children.Add(expandButton);
                grid.Children.Add(iconAndNameContainer);

                return grid;
            });
        }

        private void RefreshHierarchyVisibility()
        {
            var entitiesToProcess = _entities.ToList();

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

                int index = FindIndex(_entities, e => e.Id == entity.Id);
                if (index >= 0)
                {
                    updates.Add((index, updatedEntity));
                }
            }

            foreach (var (index, updatedEntity) in updates)
            {
                if (index < _entities.Count)
                {
                    _entities[index] = updatedEntity;
                }
            }

            var visibleEntities = _entities.Where(e => e.IsVisible).ToList();
            _entitiesList.ItemsSource = null;
            _entitiesList.ItemsSource = visibleEntities;
        }

        public void SetParent(uint childId, uint? parentId)
        {
            int childIndex = FindIndex(_entities, e => e.Id == childId);
            if (childIndex < 0) return;

            var child = _entities[childIndex];

            if (child.ParentId == parentId) return;

            int oldIndex = childIndex;
            uint? oldParentId = child.ParentId;

            var descendants = GatherDescendants(childId);

            _entities.RemoveAt(childIndex);
            foreach (var descendant in descendants)
            {
                int index = FindIndex(_entities, e => e.Id == descendant.Id);
                if (index >= 0)
                    _entities.RemoveAt(index);
            }

            if (child.ParentId != null)
            {
                int oldParentIndex = FindIndex(_entities, e => e.Id == child.ParentId);
                if (oldParentIndex >= 0)
                {
                    var oldParent = _entities[oldParentIndex];
                    oldParent.Children.Remove(childId);
                    _entities[oldParentIndex] = oldParent;
                }
            }

            int newLevel = 0;
            int targetIndex = -1;

            if (parentId != null)
            {
                int parentIndex = FindIndex(_entities, e => e.Id == parentId);
                if (parentIndex >= 0)
                {
                    var parent = _entities[parentIndex];

                    if (!IsValidParent(childId, parentId))
                    {
                        _entities.Insert(childIndex, child);
                        foreach (var descendant in descendants)
                        {
                            int insertIndex = childIndex + 1;
                            _entities.Insert(insertIndex, descendant);
                        }
                        return;
                    }

                    parent.Children.Add(childId);
                    _entities[parentIndex] = parent;
                    newLevel = parent.Level + 1;

                    targetIndex = parentIndex + 1;
                    var lastChildIndex = FindLastDescendantIndex(parentId.Value);
                    if (lastChildIndex > parentIndex)
                    {
                        targetIndex = lastChildIndex + 1;
                    }
                }
            }

            var updatedChild = child;
            updatedChild.ParentId = parentId;
            updatedChild.Level = newLevel;

            if (targetIndex >= 0 && targetIndex <= _entities.Count)
            {
                _entities.Insert(targetIndex, updatedChild);
            }
            else
            {
                _entities.Add(updatedChild);
                targetIndex = _entities.Count - 1;
            }

            int newIndex = targetIndex;

            targetIndex++;
            int levelDelta = newLevel - (child.Level - 1);

            var updatedDescendants = new List<EntityHierarchyItem>();
            foreach (var descendant in descendants)
            {
                var updatedDescendant = descendant;
                updatedDescendant.Level += levelDelta;
                updatedDescendants.Add(updatedDescendant);
            }

            foreach (var updatedDescendant in updatedDescendants)
            {
                if (targetIndex < _entities.Count)
                    _entities.Insert(targetIndex++, updatedDescendant);
                else
                    _entities.Add(updatedDescendant);
            }

            _entitiesList.SelectedItem = updatedChild;

            EntityReordered?.Invoke(this, new EntityReorderEventArgs(updatedChild, oldIndex, newIndex, parentId, oldParentId));

            UpdateChildrenLevels(childId, newLevel);
            RefreshHierarchyVisibility();
        }

        private List<EntityHierarchyItem> GatherDescendants(uint entityId)
        {
            var result = new List<EntityHierarchyItem>();
            var queue = new Queue<uint>();
            queue.Enqueue(entityId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                if (currentId != entityId)
                {
                    var entity = _entities.FirstOrDefault(e => e.Id == currentId);
                    if (entity != EntityHierarchyItem.Null)
                        result.Add(entity);
                }

                var directChildren = _entities
                    .Where(e => e.ParentId == currentId)
                    .OrderBy(e => FindIndex(_entities, x => x.Id == e.Id))
                    .ToList();

                foreach (var child in directChildren)
                {
                    queue.Enqueue(child.Id);
                }
            }

            return result;
        }

        private int FindLastDescendantIndex(uint entityId)
        {
            int lastIndex = FindIndex(_entities, e => e.Id == entityId);

            foreach (var entity in _entities)
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

        private bool IsValidParent(uint childId, uint? parentId)
        {
            if (parentId == null) return true;
            if (childId == parentId) return false;

            uint? currentParent = parentId;
            while (currentParent != null)
            {
                if (currentParent == childId) return false;

                int parentIndex = FindIndex(_entities, e => e.Id == currentParent);
                if (parentIndex < 0) break;

                currentParent = _entities[parentIndex].ParentId;
            }

            return true;
        }

        private void UpdateChildrenLevels(uint parentId, int parentLevel)
        {
            var entities = _entities.ToList();
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.ParentId == parentId)
                {
                    var updatedEntity = entity;
                    updatedEntity.Level = parentLevel + 1;
                    int entityIndex = FindIndex(_entities, e => e.Id == entity.Id);
                    if (entityIndex >= 0)
                    {
                        _entities[entityIndex] = updatedEntity;
                        UpdateChildrenLevels(updatedEntity.Id, updatedEntity.Level);
                    }
                }
            }
        }

        private void OnHierarchyPointerPressed(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);

            if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                _backgroundContextMenu.Open(this);
                e.Handled = true;
            }
            else
            {
                _backgroundContextMenu.Close();
            }
        }

        private void OnEntitiesListDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                StartRenaming(selectedEntity);
            }
        }

        public void CreateNewEntity(string name, bool withAvoking = true)
        {
            if (withAvoking) 
                EntityCreated?.Invoke(this, name);
        }
        public void SelectEntity(uint entityId)
        {
            var entityItem = _entities.FirstOrDefault(e => e.Id == entityId);
            if (entityItem != EntityHierarchyItem.Null)
            {
                _entitiesList.SelectedItem = entityItem;
                EntitySelected?.Invoke(this, entityItem);
            }
        }

        public EntityHierarchyItem CreateHierarchyEntity(EntityData entityData)
        {
            var entityItem = new EntityHierarchyItem(entityData.Id, entityData.Version, entityData.Name);

            _entities.Add(entityItem);
            _entitiesList.SelectedItem = entityItem;
             
            RefreshHierarchyVisibility();

            return entityItem;
        }

        private string GetUniqueName(string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (_entities.Any(e => e.Name == name))
            {
                name = $"{baseName} ({counter})";
                counter++;
            }
            return name;
        }

        private void StartRenaming(EntityHierarchyItem entity)
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

            this.Children.Add(popup);

            textBox.Focus();

            textBox.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        entity.Name = textBox.Text;
                        EntityRenamed?.Invoke(this, entity);

                        if (_entitiesList.ItemsSource is ObservableCollection<EntityHierarchyItem> collection)
                        {
                            var index = collection.IndexOf(entity);
                            if (index != -1)
                            {
                                collection.RemoveAt(index);
                                collection.Insert(index, entity);
                            }
                        }
                    }
                    popup.IsOpen = false;
                    this.Children.Remove(popup);
                }
                else if (e.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    this.Children.Remove(popup);
                }
            };

            textBox.LostFocus += (s, e) => {
                popup.IsOpen = false;
                this.Children.Remove(popup);
            };
        }

        private void DuplicateEntity(EntityHierarchyItem entity)
        {
            EntityDuplicated?.Invoke(this, entity);
        }

        private void DeleteEntity(EntityHierarchyItem entity)
        {
            _entities.Remove(entity);
            EntityDeleted?.Invoke(this, entity);
        }

        public void ClearEntities()
        {
            _entities.Clear();
        }

        public void UpdateHyerarchy(ProjectScene currentScene)
        {
            _currentScene = currentScene;
            Redraw();
        }

        public void Dispose() { }
        public void Redraw()
        {
            ClearEntities();
            if (isOpen && _currentScene != null)
            {
                Dictionary<uint, EntityHierarchyItem> entityMap = new Dictionary<uint, EntityHierarchyItem>();

                foreach (var entityData in _currentScene.CurrentWorldData.Entities)
                {
                    var hierarchyItem = CreateHierarchyEntity(entityData);
                    entityMap[entityData.Id] = hierarchyItem;
                }

                RefreshHierarchyVisibility();
            }
        }

        public void Open()
        {
            isOpen = true;
            Redraw();

            _entitiesList.DoubleTapped += OnEntitiesListDoubleTapped;
        }
        public void Close()
        {
            isOpen = false;

            _entitiesList.DoubleTapped -= OnEntitiesListDoubleTapped;

            OnClose?.Invoke(this);
        }
        
        private void SelectCallback(object? sender, SelectionChangedEventArgs t)
        {
            foreach (var entity in t.RemovedItems)
            {
                if (entity is EntityHierarchyItem selectedEntity)
                    Select.DeSelect(selectedEntity.Id);
            }
            foreach (var entity in t.AddedItems)
            {
                if (entity is EntityHierarchyItem selectedEntity)
                    Select.SelectItem(selectedEntity.Id);
            }
        }

        private void OnEntityListItemPointerReleased(object? sender, PointerReleasedEventArgs e)
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
                        EntitySelected?.Invoke(this, entityItem);

                        _entityContextMenu.Open(this);
                        e.Handled = true;
                    }
                }
            }
        }


        #region Commands
        private void CreateNewEntity() => CreateNewEntity(GetUniqueName("New Entity"));
        private void CreateCube() => CreateNewEntity(GetUniqueName("Cube"));
        private void CreateSphere() => CreateNewEntity(GetUniqueName("Sphere"));
        private void CreateCapsule() => CreateNewEntity(GetUniqueName("Capsule"));
        private void CreateCylinder() => CreateNewEntity(GetUniqueName("Cylinder"));
        private void CreatePlane() => CreateNewEntity(GetUniqueName("Plane"));
        private void StartRenamingCommand()
        {
            if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                StartRenaming(selectedEntity);
            }
        }
        private void DuplicateEntityCommand()
        {
            if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                DuplicateEntity(selectedEntity);
            }
        }
        private void DeleteEntityCommand()
        {
            if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                DeleteEntity(selectedEntity);
            }
        }
        #endregion

        #region DragNDrop

        private EntityHierarchyItem? _draggedItem;
        private int _draggedIndex = -1;
        private bool _isDragging = false;
        private int _dropTargetIndex = -1;
        private Border _dropIndicator;
        private uint? _targetParentId;
        private bool _asChild;

        private void OnEntityPointerPressed(object? sender, PointerPressedEventArgs e)
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
                        _selectedFile = item;
                        EntitySelected?.Invoke(this, item);
                    }
                }
            }
        }

        private void OnEntityPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedIndex != -1 && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    this.Cursor = new Cursor(StandardCursorType.DragMove);
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

        private void CalculateDropPosition(Point point)
        {
            if (_entitiesList.ItemsSource == null) return;

            var listItems = _entitiesList.ItemsSource.Cast<EntityHierarchyItem>().ToList();
            var newDropTargetIndex = -1;
            uint? newParentId = null;
            bool asChild = false;

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
                        var itemIndent = currentItem.Level * 10 + 20; // Уменьшенный отступ, как мы его настроили ранее

                        // Определяем, хочет ли пользователь вставить элемент как дочерний
                        if (horizontalPos > itemIndent + 10 && horizontalPos < itemIndent + 30)
                        {
                            newParentId = currentItem.Id;
                            asChild = true;
                            newDropTargetIndex = i;
                        }
                        else
                        {
                            // Определяем подходящего родителя исходя из горизонтальной позиции
                            int targetLevel = (int)(horizontalPos / 10);

                            // Ограничиваем targetLevel, чтобы он не мог быть больше чем level текущего элемента
                            targetLevel = Math.Min(targetLevel, currentItem.Level);

                            // Если мы хотим вставить на том же уровне или выше, ищем подходящего родителя
                            if (targetLevel <= currentItem.Level)
                            {
                                // Находим ближайшего предка с нужным уровнем
                                uint? ancestorId = currentItem.ParentId;
                                int currentLevel = currentItem.Level - 1;

                                // Ищем родителя нужного уровня
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

                            // Определяем, вставлять до или после текущего элемента
                            if (point.Y < containerTopInList + (containerBounds.Height / 2))
                            {
                                newDropTargetIndex = i;
                            }
                            else
                            {
                                newDropTargetIndex = i + 1;

                                // Если текущий элемент имеет потомков и развернут, нужно перейти через всех потомков
                                if (currentItem.IsExpanded && currentItem.Children.Count > 0)
                                {
                                    var lastDescendantIndex = FindLastVisibleDescendantIndex(currentItem.Id, listItems);
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
                            var parentItem = _entities.FirstOrDefault(e => e.Id == newParentId);
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
            foreach (var entity in _entities)
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

        private void OnEntityPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging && _draggedItem != null && _dropTargetIndex != -1)
            {
                if (_asChild && _targetParentId != null)
                {
                    SetParent(_draggedItem.Value.Id, _targetParentId);
                }
                else if (_dropTargetIndex != _draggedIndex && (_dropTargetIndex != _draggedIndex + 1 || _draggedItem.Value.ParentId != _targetParentId))
                {
                    ReorderEntity(_draggedItem.Value, _dropTargetIndex, _targetParentId);
                }
                _selectedFile = _draggedItem.Value;
            }
            else if (_draggedItem != null && !_isDragging)
            {
                var visual = e.Source as Visual;
                if (visual != null)
                {
                    if (visual.DataContext is EntityHierarchyItem clickedItem)
                    {
                        if (clickedItem != null && clickedItem == _selectedFile)
                        {
                            _selectedFile = EntityHierarchyItem.Null;
                            EntitySelected?.Invoke(this, clickedItem);
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
            this.Cursor = Cursor.Default;
        }

        private void UpdateChildrenVisibility(uint parentId, bool isVisible)
        {
            var entities = _entities.ToList();
            var entitiesToUpdate = new List<(int index, EntityHierarchyItem entity)>();

            foreach (var entity in entities)
            {
                if (entity.ParentId == parentId)
                {
                    var updatedEntity = entity;
                    updatedEntity.IsVisible = isVisible;

                    int index = FindIndex(_entities, e => e.Id == entity.Id);
                    if (index >= 0)
                    {
                        entitiesToUpdate.Add((index, updatedEntity));
                    }

                    if (entity.Children.Count > 0)
                    {
                        UpdateChildrenVisibility(entity.Id, isVisible && entity.IsExpanded);
                    }
                }
            }

            foreach (var (index, updatedEntity) in entitiesToUpdate)
            {
                if (index < _entities.Count)
                {
                    _entities[index] = updatedEntity;
                }
            }

            RefreshList();
        }

        private void RefreshList()
        {
            var visibleEntities = _entities.Where(e => e.IsVisible).ToList();
            _entitiesList.ItemsSource = null;
            _entitiesList.ItemsSource = visibleEntities;
        }

        private void ReorderEntity(EntityHierarchyItem item, int newIndex, uint? newParentId = null)
        {
            int currentIndex = FindIndex(_entities, e => e.Id == item.Id && e.Version == item.Version);
            if (currentIndex < 0) return;

            var descendants = GatherDescendants(item.Id);
            if (item.ParentId != newParentId)
            {
                SetParent(item.Id, newParentId);
                return;
            }
            var itemsToRemove = new List<uint> { item.Id };
            itemsToRemove.AddRange(descendants.Select(d => d.Id));

            var removedItems = new List<EntityHierarchyItem>();
            foreach (var id in itemsToRemove)
            {
                int index = FindIndex(_entities, e => e.Id == id);
                if (index >= 0)
                {
                    removedItems.Add(_entities[index]);
                    _entities.RemoveAt(index);

                    if (index < newIndex)
                        newIndex--;
                }
            }

            if (newIndex >= 0 && newIndex <= _entities.Count)
            {
                _entities.Insert(newIndex, item);
            }
            else
            {
                _entities.Add(item);
                newIndex = _entities.Count - 1;
            }

            newIndex++;
            foreach (var descendant in descendants)
            {
                if (newIndex < _entities.Count)
                    _entities.Insert(newIndex++, descendant);
                else
                    _entities.Add(descendant);
            }

            _entitiesList.SelectedItem = item;
            EntityReordered?.Invoke(this, new EntityReorderEventArgs(item, currentIndex, newIndex, newParentId));
            RefreshHierarchyVisibility();
        }
        #endregion
        private int FindLastVisibleDescendantIndex(uint entityId, List<EntityHierarchyItem> visibleItems)
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

    public class EntityReorderEventArgs : EventArgs
    {
        public EntityHierarchyItem Entity { get; }
        public int OldIndex { get; }
        public int NewIndex { get; }
        public uint? NewParentId { get; }
        public uint? OldParentId { get; }

        public EntityReorderEventArgs(EntityHierarchyItem entity, int oldIndex, int newIndex, uint? newParentId = null, uint? oldParentId = null)
        {
            Entity = entity;
            OldIndex = oldIndex;
            NewIndex = newIndex;
            NewParentId = newParentId;
            OldParentId = oldParentId;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    public struct EntityHierarchyItem : INotifyPropertyChanged, IEquatable<EntityHierarchyItem>
    {
        private bool isNull = true;
        private Entity _entity;

        public event PropertyChangedEventHandler? PropertyChanged;

        public EntityHierarchyItem(uint id, uint version, string name)
        {
            _entity = new Entity(id, Version);
            Name = name;
            isNull = false;
            ParentId = null;
            Children = new List<uint>();
            IsExpanded = true;
            Level = 0;
        }
        public EntityHierarchyItem(Entity entity, string name)
        {
            Name = name;
            _entity = entity;
            isNull = false;
            ParentId = null;
            Children = new List<uint>();
            IsExpanded = true;
            Level = 0;
        }

        public uint Id => _entity.Id;
        public uint Version => _entity.Version;
        public Entity EntityReference => _entity;
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private uint? _parentId;
        public uint? ParentId
        {
            get => _parentId;
            set
            {
                if (_parentId != value)
                {
                    _parentId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentId)));
                }
            }
        }

        private List<uint> _children;
        public List<uint> Children
        {
            get => _children;
            set
            {
                _children = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }
        }

        private int _level;
        public int Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
                }
            }
        }

        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public override int GetHashCode() => Id.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is EntityHierarchyItem other)
            {
                return Id == other.Id && Version == other.Version;
            }
            return false;
        }

        public override string ToString() =>
            $"{_entity} Name:{Name} IsActive:{IsActive} IsVisible:{IsVisible} Level:{Level}";

        public bool Equals(EntityHierarchyItem other) => Id == other.Id && Version == other.Version && isNull == other.isNull;
        public static EntityHierarchyItem Null => new EntityHierarchyItem() { isNull = true };
        public static bool operator ==(EntityHierarchyItem left, EntityHierarchyItem right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(EntityHierarchyItem left, EntityHierarchyItem right)
            => !(left == right);
    }
    
}