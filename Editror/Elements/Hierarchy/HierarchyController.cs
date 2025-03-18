using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using AtomEngine;
using System;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia;

namespace Editor
{
    internal class HierarchyController : Grid, IWindowed, ICacheble
    {
        public Action<object> OnClose { get; set; }

        private ObservableCollection<EntityHierarchyItem> _entities;
        private ProjectScene _currentScene;
        private ListBox _entitiesList;
        private Canvas _indicatorCanvas;
        private Border _dropIndicator;
        private bool isOpen = false;
         
        public event EventHandler<String> EntityCreated;
        public event EventHandler<EntityHierarchyItem> EntitySelected;
        public event EventHandler<EntityHierarchyItem> EntityDuplicated;
        public event EventHandler<EntityHierarchyItem> EntityRenamed;
        public event EventHandler<EntityHierarchyItem> EntityDeleted;
        public event EventHandler<EntityReorderEventArgs> EntityReordered;

        private SceneManager _sceneManager;
        private EntityHierarchyItem _selectedItem = EntityHierarchyItem.Null;

        private HierarchyUIBuilder _uiBuilder;
        private HierarchyDataManager _dataManager;
        private HierarchyDragDropHandler _dragDropHandler;
        private EntityHierarchyOperations _operations;
        private MenuProvider _menuProvider;
        private ModelDragDropHandler _modelDragDropHandler;

        private Border _modelDropIndicator;

        public ObservableCollection<EntityHierarchyItem> Entities => _entities;
        public ListBox EntitiesList => _entitiesList;
        public ProjectScene CurrentScene => _currentScene;
        public SceneManager SceneManager => _sceneManager;
        public EntityHierarchyItem SelectedItem { get => _selectedItem; set => _selectedItem = value; }

        public HierarchyController()
        {
            _entities = new ObservableCollection<EntityHierarchyItem>();

            _uiBuilder = new HierarchyUIBuilder(this);
            _dataManager = new HierarchyDataManager(this);
            _operations = new EntityHierarchyOperations(this);
            _menuProvider = new MenuProvider(this);

            InitializeUI();
            InitializeModelDragDrop();

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
            _uiBuilder.InitializeGrid(this);

            _entitiesList = _uiBuilder.EntitiesList;
            _indicatorCanvas = _uiBuilder.IndicatorCanvas;
            _dropIndicator = _uiBuilder.DropIndicator;

            var backgroundMenu = _menuProvider.CreateBackgroundContextMenu();
            var entityMenu = _menuProvider.CreateEntityContextMenu();

            _dragDropHandler = new HierarchyDragDropHandler(this, _entitiesList, _indicatorCanvas, _dropIndicator);
            _dragDropHandler.EntityReordered += (s, e) => EntityReordered?.Invoke(s, e);

            _entitiesList.SelectionChanged += SelectCallback;

            _entitiesList.AddHandler(InputElement.PointerReleasedEvent, _menuProvider.OnEntityContextMenuRequested, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerReleasedEvent, _dragDropHandler.OnEntityListItemPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerPressedEvent, _dragDropHandler.OnEntityPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerMovedEvent, _dragDropHandler.OnEntityPointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _entitiesList.AddHandler(InputElement.PointerReleasedEvent, _dragDropHandler.OnEntityPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);

            PointerReleased += _menuProvider.OnHierarchyPointerPressed;
        }

        private void InitializeModelDragDrop()
        {
            _modelDragDropHandler = new ModelDragDropHandler(this);

            DragDrop.SetAllowDrop(this, true);
            DragDrop.SetAllowDrop(_entitiesList, true);

            //this.AddHandler(DragDrop.DragOverEvent, OnModelDragOver);
            this.AddHandler(DragDrop.DropEvent, OnModelDrop);

            //_entitiesList.AddHandler(DragDrop.DragOverEvent, OnModelDragOver);
            //_entitiesList.AddHandler(DragDrop.DropEvent, OnModelDrop);

            _indicatorCanvas.AddHandler(DragDrop.DragOverEvent, OnModelDragOver);
            _indicatorCanvas.AddHandler(DragDrop.DropEvent, OnModelDrop);
            

            _modelDropIndicator = new Border
            {
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.DodgerBlue),
                Background = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255)),
                CornerRadius = new CornerRadius(3),
                IsVisible = false,
                ZIndex = 1000
            };

            _indicatorCanvas.Children.Add(_modelDropIndicator);

            this.AddHandler(DragDrop.DragEnterEvent, OnModelDragEnter);
            this.AddHandler(DragDrop.DragLeaveEvent, OnModelDragLeave);

            _indicatorCanvas.AddHandler(DragDrop.DragEnterEvent, OnModelDragEnter);
            _indicatorCanvas.AddHandler(DragDrop.DragLeaveEvent, OnModelDragLeave);
            //_entitiesList.AddHandler(DragDrop.DragLeaveEvent, OnModelDragLeave);
        }

        private void OnModelDragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy;

            if (CanAcceptModelDrop(e))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void OnModelDrop(object sender, DragEventArgs e)
        {
            _modelDropIndicator.IsVisible = false;

            if (CanAcceptModelDrop(e))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(
                        jsonData, GlobalDeserializationSettings.Settings);

                    _modelDragDropHandler.HandleModelDrop(fileEvent);
                    Status.SetStatus($"Модель '{fileEvent.FileName}' добавлена в иерархию");
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при обработке перетаскивания: {ex.Message}");
                }
            }

            e.Handled = true;
        }

        private void OnModelDragEnter(object sender, DragEventArgs e)
        {
            if (CanAcceptModelDrop(e))
            {
                var bounds = _entitiesList.Bounds;
                var position = _entitiesList.TranslatePoint(new Point(0, 0), _indicatorCanvas);

                if (position.HasValue)
                {
                    Canvas.SetLeft(_modelDropIndicator, position.Value.X);
                    Canvas.SetTop(_modelDropIndicator, position.Value.Y);
                    _modelDropIndicator.Width = bounds.Width;
                    _modelDropIndicator.Height = bounds.Height;
                    _modelDropIndicator.IsVisible = true;
                }
            }
        }

        private void OnModelDragLeave(object sender, DragEventArgs e)
        {
            _modelDropIndicator.IsVisible = false;
        }

        private bool CanAcceptModelDrop(DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(
                            jsonData, GlobalDeserializationSettings.Settings);

                        if (fileEvent != null)
                        {
                            string extension = fileEvent.FileExtension?.ToLowerInvariant();
                            if (!string.IsNullOrEmpty(extension))
                            {
                                string[] modelExtensions = { ".obj", ".fbx", ".3ds", ".blend" };
                                return modelExtensions.Contains(extension);
                            }
                        }
                    }
                }
                catch { }
            }
            return false;
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
            var entityItem = _operations.CreateHierarchyEntity(entityData);
            RefreshHierarchyVisibility();
            return entityItem;
        }

        public void ClearEntities()
        {
            _entities.Clear();
        }

         
        public void Dispose() { }

        public void Redraw()
        {
            if (isOpen && _currentScene != null)
            {
                _dataManager.BuildHierarchyFromComponents();
            }
        }

        public void Open()
        {
            isOpen = true;
            _dataManager.BuildHierarchyFromComponents();

            _entitiesList.DoubleTapped += _operations.OnEntitiesListDoubleTapped;
        }

        public void Close()
        {
            isOpen = false;

            _entitiesList.DoubleTapped -= _operations.OnEntitiesListDoubleTapped;

            OnClose?.Invoke(this);
        }

        public void UpdateHyerarchy(ProjectScene currentScene)
        {
            _currentScene = currentScene;
            if (isOpen)
            {
                _dataManager.BuildHierarchyFromComponents();
            }
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

         
        public void RefreshHierarchyVisibility()
        {
            _dataManager.RefreshHierarchyVisibility();
        }

        public void SetParent(uint childId, uint? parentId)
        {
            _operations.SetParent(childId, parentId, out var eventArgs);
            if (eventArgs != null)
                EntityReordered?.Invoke(this, eventArgs);
        }

         
        public void OnEntitySelected(EntityHierarchyItem entity)
        {
            EntitySelected?.Invoke(this, entity);
        }

        public void OnEntityRenamed(EntityHierarchyItem entity)
        {
            EntityRenamed?.Invoke(this, entity);
        }

        public void OnEntityDeleted(EntityHierarchyItem entity)
        {
            EntityDeleted?.Invoke(this, entity);
        }

        public void OnEntityDuplicated(EntityHierarchyItem entity)
        {
            EntityDuplicated?.Invoke(this, entity);
        }

        public void FreeCache()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                _currentScene = null;
            }));
        }
    }

}