using System.Collections.ObjectModel;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using AtomEngine;
using System;
using EngineLib;
using System.Collections.Generic;

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
            _modelDropIndicator = _uiBuilder.ModelDropIndicator;

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
            _modelDragDropHandler = new ModelDragDropHandler(this, _entitiesList, _indicatorCanvas, _modelDropIndicator);
            _modelDragDropHandler.Initialize();
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
            var index = _entities.IndexOf(entity);
            if (index != -1)
            {
                _entities[index] = entity;
            }
            RefreshHierarchyVisibility();
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
            EditorSetter.Invoke(() =>
            {
                _currentScene = null;
            });
        }
    }

}