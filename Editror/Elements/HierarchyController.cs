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

        private SceneManager _sceneManager;


        public HierarchyController()
        {
            _entities = new ObservableCollection<EntityHierarchyItem>();
            InitializeUI();

            _entityContextMenu = CreateEntityComtexMenu();
            _backgroundContextMenu = CreateBGContextMenus();

            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.OnSceneInitialize += UpdateHyerarchy;

            _sceneManager.OnEntityCreated += (worldId, entityId) =>
            {
                var entityData = _sceneManager.CurrentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityId);
                if (entityData != null)
                {
                    CreateHierarchyEntity(entityData, false);
                }
            };
            _sceneManager.OnEntityDuplicated += (worldId, entityIdFrom, entityIdTo) =>
            {
                var entityData = _sceneManager.CurrentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityIdTo);
                if (entityData != null)
                {
                    CreateHierarchyEntity(entityData, false);
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
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var iconAndNameContainer = new StackPanel
                {
                    Classes = { "entityCell" }
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

                grid.Children.Add(iconAndNameContainer);

                return grid;
            });
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

        public EntityHierarchyItem CreateHierarchyEntity(EntityData entityData, bool withAvoking = true)
        {
            var entityItem = new EntityHierarchyItem(entityData.Id, entityData.Version, entityData.Name);

            _entities.Add(entityItem);
            _entitiesList.SelectedItem = entityItem;
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
                for (int i = 0; i < _currentScene.CurrentWorldData.Entities.Count(); i++)
                {
                    CreateHierarchyEntity(_currentScene.CurrentWorldData.Entities[i], false);
                }
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
        
        private EntityHierarchyItem _selectedFile = EntityHierarchyItem.Null;
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

            for (int i = 0; i < listItems.Count; i++)
            {
                var container = _entitiesList.ContainerFromIndex(i) as Control;
                if (container != null)
                {
                    var containerBounds = container.Bounds;
                    var containerTopInList = container.TranslatePoint(new Point(0, 0), _entitiesList)?.Y ?? 0;

                    if (point.Y >= containerTopInList && point.Y < containerTopInList + containerBounds.Height)
                    {
                        if (point.Y < containerTopInList + (containerBounds.Height / 2))
                        {
                            newDropTargetIndex = i;
                        }
                        else
                        {
                            newDropTargetIndex = i + 1;
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
                    }
                }
            }
            if (newDropTargetIndex == _draggedIndex || newDropTargetIndex == _draggedIndex + 1)
            {
                _dropIndicator.IsVisible = false;
                _dropTargetIndex = -1;
                return;
            }

            _dropTargetIndex = newDropTargetIndex;

            if (_dropTargetIndex >= 0)
            {
                _dropIndicator.IsVisible = true;

                double indicatorY;
                if (_dropTargetIndex >= listItems.Count)
                {
                    var lastContainer = _entitiesList.ContainerFromIndex(listItems.Count - 1) as Control;
                    if (lastContainer != null)
                    {
                        var lastContainerBottom = lastContainer.TranslatePoint(new Point(0, lastContainer.Bounds.Height), _entitiesList)?.Y ?? 0;
                        indicatorY = lastContainerBottom;
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
                    }
                    else
                    {
                        _dropIndicator.IsVisible = false;
                        return;
                    }
                }

                var listBoxPoint = _entitiesList.TranslatePoint(new Point(0, indicatorY), _indicatorCanvas) ?? new Point(0, 0);

                _dropIndicator.Width = _entitiesList.Bounds.Width;
                Canvas.SetTop(_dropIndicator, listBoxPoint.Y - _dropIndicator.Height / 2);
                Canvas.SetLeft(_dropIndicator, 0); 
            }
            else
            {
                _dropIndicator.IsVisible = false;
            }
        }

        private void OnEntityPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging && _draggedItem != null && _dropTargetIndex != -1 && _dropTargetIndex != _draggedIndex && _dropTargetIndex != _draggedIndex + 1)
            {
                ReorderEntity(_draggedItem.Value, _dropTargetIndex);
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
            _dropIndicator.IsVisible = false;
            this.Cursor = Cursor.Default;
        }

        private void ReorderEntity(EntityHierarchyItem item, int newIndex)
        {
            int currentIndex = _entities.IndexOf(item);

            if (currentIndex < 0) return;

            if (newIndex > currentIndex)
            {
                newIndex--;
            }

            _entities.RemoveAt(currentIndex);
            _entities.Insert(newIndex, item);

            _entitiesList.SelectedItem = item;

            // Уведомляем о перемещении
            //EntityReordered?.Invoke(this, new EntityReorderEventArgs(item, currentIndex, newIndex));
        }
        #endregion

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
        }
        public EntityHierarchyItem(Entity entity, string name)
        {
            Name = name;
            _entity = entity;
            isNull = false;
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
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
            $"{_entity} Name:{Name} IsActive:{IsActive} IsVisible:{IsVisible}";

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