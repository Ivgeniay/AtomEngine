using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Data;
using System.Linq;
using AtomEngine;
using Avalonia;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Editor
{
    internal class HierarchyController : Grid, IWindowed
    {
        private ListBox _entitiesList;
        private ContextMenu _backgroundContextMenu;
        private ContextMenu _entityContextMenu;
        private ObservableCollection<EntityHierarchyItem> _entities;
        private uint _nextEntityId = 1;

        public Action<object> OnClose { get; set; }

        // События для взаимодействия с внешними компонентами
        public event EventHandler<EntityHierarchyItem> EntitySelected;
        public event EventHandler<EntityHierarchyItem> EntityCreated;
        public event EventHandler<EntityHierarchyItem> EntityRenamed;
        public event EventHandler<EntityHierarchyItem> EntityDeleted;


        public HierarchyController()
        {
            _entities = new ObservableCollection<EntityHierarchyItem>();
            InitializeUI();
            CreateContextMenus();
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

            // Устанавливаем источник данных и прокрутку
            _entitiesList.ItemsSource = _entities;
            _entitiesList.AutoScrollToSelectedItem = true;
            _entitiesList.SelectionMode = SelectionMode.Multiple;
            ScrollViewer.SetHorizontalScrollBarVisibility(_entitiesList, ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(_entitiesList, ScrollBarVisibility.Auto);

            _entitiesList.SelectionChanged += (s, e) => {
                if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
                {
                    CloseAllContext();
                    EntitySelected?.Invoke(this, selectedEntity);
                }
            };

            // Настройка обработчиков событий мыши
            _entitiesList.PointerPressed += OnEntitiesListPointerPressed;
            _entitiesList.DoubleTapped += OnEntitiesListDoubleTapped;
            _entitiesList.AddHandler(InputElement.PointerPressedEvent, (s, e) =>
            {
                var visual = e.Source as Visual;
                if (visual != null)
                {
                    if (visual.DataContext is EntityHierarchyItem item)
                    {
                        if (item != null && e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                        {
                            var listItems = _entitiesList.ItemsSource.Cast<EntityHierarchyItem>().ToList();
                            var index = listItems.FindIndex(e => e.Name == item.Name);

                            if (index != -1)
                            {
                                _entitiesList.Selection.Clear();
                                _entitiesList.Selection.Select(index);
                                CloseAllContext();
                                _entityContextMenu.Open(_entitiesList);
                            }
                            e.Handled = true;
                        }
                    }
                }
            }, RoutingStrategies.Tunnel);

            // Настройка шаблона для отображения элементов
            _entitiesList.ItemTemplate = CreateEntityItemTemplate();

            Grid.SetRow(_entitiesList, 1);

            // Добавляем на сетку
            Children.Add(header);
            Children.Add(_entitiesList);

            // Настраиваем обработчик нажатия на пустую область списка
            PointerPressed += OnHierarchyPointerPressed;

            _entitiesList.SelectionChanged += (e, t) =>
            {
                foreach (var entity in t.RemovedItems)
                {
                    if (entity is EntityHierarchyItem selectedEntity)
                        Select.DeSelect(selectedEntity);
                }
                foreach (var entity in t.AddedItems)
                {
                    if (entity is EntityHierarchyItem selectedEntity)
                        Select.SelectItem(selectedEntity);
                }
            };
        }

        private void CreateContextMenus()
        {
            _backgroundContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var createEntityItem = new MenuItem
            {
                Header = "Create Entity",
                Classes = { "hierarchyMenuItem" }
            };
            createEntityItem.Click += (s, e) => CreateNewEntity("New Entity");

            var createEmptyItem = new MenuItem
            {
                Header = "Create Empty",
                Classes = { "hierarchyMenuItem" }
            };
            createEmptyItem.Click += (s, e) => CreateNewEntity("Empty Entity");

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

            // Создаем подменю для 3D объектов
            var cubeItem = new MenuItem
            {
                Header = "Cube",
                Classes = { "hierarchyMenuItem" }
            };
            cubeItem.Click += (s, e) => CreateNewEntity("Cube");

            var sphereItem = new MenuItem
            {
                Header = "Sphere",
                Classes = { "hierarchyMenuItem" }
            };
            sphereItem.Click += (s, e) => CreateNewEntity("Sphere");

            var capsuleItem = new MenuItem
            {
                Header = "Capsule",
                Classes = { "hierarchyMenuItem" }
            };
            capsuleItem.Click += (s, e) => CreateNewEntity("Capsule");

            var cylinderItem = new MenuItem
            {
                Header = "Cylinder",
                Classes = { "hierarchyMenuItem" }
            };
            cylinderItem.Click += (s, e) => CreateNewEntity("Cylinder");

            var planeItem = new MenuItem
            {
                Header = "Plane",
                Classes = { "hierarchyMenuItem" }
            };
            planeItem.Click += (s, e) => CreateNewEntity("Plane");

            ((ItemsControl)transform3dItem).Items.Add(cubeItem);
            ((ItemsControl)transform3dItem).Items.Add(sphereItem);
            ((ItemsControl)transform3dItem).Items.Add(capsuleItem);
            ((ItemsControl)transform3dItem).Items.Add(cylinderItem);
            ((ItemsControl)transform3dItem).Items.Add(planeItem);

            // Подменю для 2D объектов
            var transform2dItem = new MenuItem
            {
                Header = "2D Object",
                Classes = { "hierarchyMenuItem" }
            };

            var spriteItem = new MenuItem
            {
                Header = "Sprite",
                Classes = { "hierarchyMenuItem" }
            };
            spriteItem.Click += (s, e) => CreateNewEntity("Sprite");

            var textItem = new MenuItem
            {
                Header = "Text",
                Classes = { "hierarchyMenuItem" }
            };
            textItem.Click += (s, e) => CreateNewEntity("Text");

            var buttonItem = new MenuItem
            {
                Header = "Button",
                Classes = { "hierarchyMenuItem" }
            };
            buttonItem.Click += (s, e) => CreateNewEntity("Button");

            var panelItem = new MenuItem
            {
                Header = "Panel",
                Classes = { "hierarchyMenuItem" }
            };
            panelItem.Click += (s, e) => CreateNewEntity("Panel");

            ((ItemsControl)transform2dItem).Items.Add(spriteItem);
            ((ItemsControl)transform2dItem).Items.Add(textItem);
            ((ItemsControl)transform2dItem).Items.Add(buttonItem);
            ((ItemsControl)transform2dItem).Items.Add(panelItem);

            ((ItemsControl)_backgroundContextMenu).Items.Add(createEntityItem);
            ((ItemsControl)_backgroundContextMenu).Items.Add(createEmptyItem);
            ((ItemsControl)_backgroundContextMenu).Items.Add(separatorItem);
            ((ItemsControl)_backgroundContextMenu).Items.Add(transform3dItem);
            ((ItemsControl)_backgroundContextMenu).Items.Add(transform2dItem);

            // Контекстное меню для сущности
            _entityContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var renameItem = new MenuItem
            {
                Header = "Rename",
                Classes = { "hierarchyMenuItem" }
            };
            renameItem.Click += (s, e) => {
                if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
                {
                    StartRenaming(selectedEntity);
                }
            };

            var duplicateItem = new MenuItem
            {
                Header = "Duplicate",
                Classes = { "hierarchyMenuItem" }
            };
            duplicateItem.Click += (s, e) => {
                if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
                {
                    DuplicateEntity(selectedEntity);
                }
            };

            var deleteItem = new MenuItem
            {
                Header = "Delete",
                Classes = { "hierarchyMenuItem" }
            };
            deleteItem.Click += (s, e) => {
                if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
                {
                    DeleteEntity(selectedEntity);
                }
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
            var audioItem = new MenuItem
            {
                Header = "Audio",
                Classes = { "hierarchyMenuItem" }
            };
            var inputItem = new MenuItem
            {
                Header = "Input",
                Classes = { "hierarchyMenuItem" }
            };
            var navigationItem = new MenuItem
            {
                Header = "Navigation",
                Classes = { "hierarchyMenuItem" }
            };

            ((ItemsControl)addComponentItem).Items.Add(physicsItem);
            ((ItemsControl)addComponentItem).Items.Add(renderingItem);
            ((ItemsControl)addComponentItem).Items.Add(audioItem);
            ((ItemsControl)addComponentItem).Items.Add(inputItem);
            ((ItemsControl)addComponentItem).Items.Add(navigationItem);

            ((ItemsControl)_entityContextMenu).Items.Add(renameItem);
            ((ItemsControl)_entityContextMenu).Items.Add(duplicateItem);
            ((ItemsControl)_entityContextMenu).Items.Add(deleteItem);
            ((ItemsControl)_entityContextMenu).Items.Add(entitySeparator);
            ((ItemsControl)_entityContextMenu).Items.Add(addComponentItem);
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


        private void OnHierarchyPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);

            if (point.Properties.IsRightButtonPressed && !HitTestListBoxItems(e.GetPosition(_entitiesList)))
            {
                CloseAllContext();
                _backgroundContextMenu.Open(this);
                e.Handled = true;
            }
        }

        private void OnEntitiesListPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(_entitiesList);

            if (point.Properties.IsRightButtonPressed)
            {
                CloseAllContext();
                _backgroundContextMenu.Open(this);
                e.Handled = true;
            }
        }
        
        private void OnEntitiesListDoubleTapped(object sender, RoutedEventArgs e)
        {
            if (_entitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                StartRenaming(selectedEntity);
            }
        }

        private bool HitTestListBoxItems(Point point)
        {
            return FindItemByPosition(_entitiesList, point) != null;
        }

        private EntityHierarchyItem FindItemByPosition(ListBox listBox, Point point)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                var container = listBox.ContainerFromIndex(i) as ListBoxItem;
                if (container != null)
                {
                    var bounds = container.Bounds;
                    if (bounds.Contains(point))
                    {
                        return _entities[i];
                    }
                }
            }
            return default;
        }


        public void CreateNewEntity(string name, bool withAvoking = true)
        {
            var entity = new Entity(_nextEntityId++, 1);
            var entityItem = new EntityHierarchyItem(entity, GetUniqueName(name));

            _entities.Add(entityItem);
            if (withAvoking) EntityCreated?.Invoke(this, entityItem);
            _entitiesList.SelectedItem = entityItem;
        }

        public void CreateEntityHierarchy(EntityData entityData, bool withAvoking = true)
        {
            var entityItem = new EntityHierarchyItem(entityData.Id, entityData.Version, entityData.Name);

            _entities.Add(entityItem);
            if (withAvoking) EntityCreated?.Invoke(this, entityItem);
            _entitiesList.SelectedItem = entityItem;
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
            // Создаем текстовое поле для редактирования
            var textBox = new TextBox
            {
                Text = entity.Name,
                Width = 200,
                SelectionStart = 0,
                SelectionEnd = entity.Name.Length,
                Classes = { "renameTextBox" }
            };

            // Создаем Popup для отображения поля ввода
            var popup = new Popup
            {
                Child = textBox,
                Placement = PlacementMode.Pointer,
                IsOpen = true
            };

            // Добавляем Popup в визуальное дерево
            this.Children.Add(popup);

            textBox.Focus();

            // Обработчик завершения редактирования
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

            // Закрываем при потере фокуса
            textBox.LostFocus += (s, e) => {
                popup.IsOpen = false;
                this.Children.Remove(popup);
            };
        }

        private void DuplicateEntity(EntityHierarchyItem entity)
        {
            // Создаем новую сущность с уникальным ID
            var newEntity = new Entity(_nextEntityId++, 1);
            var newEntityItem = new EntityHierarchyItem(newEntity, GetUniqueName($"{entity.Name} Copy"));

            _entities.Add(newEntityItem);
            EntityCreated?.Invoke(this, newEntityItem);
            _entitiesList.SelectedItem = newEntityItem;
        }

        private void DeleteEntity(EntityHierarchyItem entity)
        {
            _entities.Remove(entity);
            EntityDeleted?.Invoke(this, entity);
        }

        // Публичные методы для управления иерархией
        public void AddEntity(EntityHierarchyItem entity)
        {
            if (!_entities.Contains(entity))
            {
                _entities.Add(entity);
            }
        }

        public void RemoveEntity(EntityHierarchyItem entity)
        {
            _entities.Remove(entity);
        }

        public void ClearEntities()
        {
            _entities.Clear();
        }

        public void SelectEntity(EntityHierarchyItem entity)
        {
            _entitiesList.SelectedItem = entity;
        }

        public void UpdateHyerarchy(ProjectScene currentScene)
        {
            ClearEntities();

            for(int i = 0; i < currentScene.CurrentWorldData.Entities.Count(); i++)
            {
                CreateEntityHierarchy(currentScene.CurrentWorldData.Entities[i], false);
            }
        }

        public void Dispose()
        {
            
        }

        private void CloseAllContext()
        {
            _entityContextMenu?.Close();
            _backgroundContextMenu?.Close();
        }
    }

    public struct EntityHierarchyItem : INotifyPropertyChanged, IEquatable<EntityHierarchyItem>
    {
        private Entity _entity;

        public event PropertyChangedEventHandler? PropertyChanged;

        public EntityHierarchyItem(uint id, uint version, string name)
        {
            _entity = new Entity(id, Version);
            Name = name;
        }
        public EntityHierarchyItem(Entity entity, string name)
        {
            Name = name;
            _entity = entity;
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

        public bool Equals(EntityHierarchyItem other) => Id == other.Id && Version == other.Version;

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