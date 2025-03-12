using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using AtomEngine;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Editor
{
    internal class SceneManager : IService
    {
        public Action<ProjectScene>? OnSceneInitialize;
        public Action<ProjectScene>? OnSceneDirty;
        public Action? OnSceneBeforeSave;
        public Action? OnSceneAfterSave;
        public Action? OnScenUnload;
        
        public Action<uint, uint, IComponent>? OnComponentAdded;
        public Action<uint, uint, IComponent>? OnComponentRemoved;
        public Action<uint, uint, IComponent>? OnComponentChange;

        public Action<uint, uint>? OnEntityCreated;
        public Action<uint, uint>? OnEntityDeleted;
        public Action<uint, uint>? OnEntityDuplicated;
        public Action<uint, uint>? OnEntityRemoved;
        public Action<uint, uint>? OnEntityRenamed;

        public Action<uint, string, string> OnWorldRename;
        public Action<uint, string> OnWorldRemove;
        public Action<uint, string> OnWorldCreate;
        public Action<uint, string> OnWorldSelected;

        internal ProjectScene CurrentScene { get => _currentScene; private set => _currentScene = value; }
        private ProjectScene _currentScene;
        private Window _mainWindow; 

        public void SetMainWindow(Window window) { 
            _mainWindow = window; 
        }

        internal void AddComponent(uint entityId, Type typeComponent)
        {
            var instance = CurrentScene.AddComponent(entityId, typeComponent);
            OnComponentAdded?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void RemoveComponent(uint entityId, Type typeComponent)
        {
            var instance = CurrentScene.RemoveComponent(entityId, typeComponent);
            OnComponentRemoved?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void ComponentChange(uint entityId, IComponent component)
        {
            OnComponentChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, component);
            OnSceneDirty?.Invoke(CurrentScene);
        }

        internal void AddEntity(string entityName)
        {
            uint id = CurrentScene.AddEntity(entityName);
            OnEntityCreated?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void AddDuplicateEntity(EntityHierarchyItem hierarchyEntity)
        {
            CurrentScene.AddDuplicateEntity(hierarchyEntity);
            OnEntityDuplicated?.Invoke(CurrentScene.CurrentWorldData.WorldId, hierarchyEntity.Id);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void RemoveEntity(EntityHierarchyItem entity)
        {
            uint id = CurrentScene.DeleteEntity(entity);
            OnEntityRemoved?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void RenameEntity(EntityHierarchyItem entity)
        {
            uint id = CurrentScene.RenameEntity(entity);
            OnEntityRenamed?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
            OnSceneDirty?.Invoke(CurrentScene);
        }

        internal void RenameWorld((string, string) worldNameLastCurrent)
        {
            CurrentScene.RenameWorld(worldNameLastCurrent);
            OnWorldRename?.Invoke(CurrentScene.CurrentWorldData.WorldId, worldNameLastCurrent.Item1, worldNameLastCurrent.Item2);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void RemoveWorld(string worldName)
        {
            var deletedWorld = CurrentScene.RemoveWorld(worldName);
            OnWorldRemove?.Invoke(deletedWorld.WorldId, worldName);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void CreateWorld(string worldName)
        {
            var newWorldData = CurrentScene.CreateWorld(worldName);
            OnWorldCreate?.Invoke(newWorldData.WorldId, worldName);
            OnSceneDirty?.Invoke(CurrentScene);
        }
        internal void SelecteWorld(string worldName)
        {
            CurrentScene.SelecteWorld(worldName);
            OnWorldSelected?.Invoke(CurrentScene.CurrentWorldData.WorldId, CurrentScene.CurrentWorldData.WorldName);
        }



        public Task InitializeAsync() => Task.CompletedTask;
        internal async Task HandleNewScene()
        {
            if (CurrentScene != null && CurrentScene.IsDirty)
            {
                var res = await ConfirmationDialog.Show(
                    _mainWindow,
                    "Worning",
                    "There are scene changes. Do you want to safe scene?",
                    true);

                switch (res)
                {
                    case ConfirmationDialog.DialogResult.Cancel:
                        return;
                    case ConfirmationDialog.DialogResult.Yes:
                        var t = await FileDialogService.SaveFileAsync(
                            _mainWindow,
                            $"Safe {CurrentScene.WorldName}",
                            $"{CurrentScene.WorldName}",
                            new FileDialogService.FileFilter("scene", "scene"));

                        if (t != null) Status.SetStatus($"{t}");
                        break;

                    case ConfirmationDialog.DialogResult.No:
                        break;
                }
            }

            OnScenUnload?.Invoke();
            WorldData standartWorldData = SceneFileHelper.CreateWorldData();
            CurrentScene = new ProjectScene(new List<WorldData>() { standartWorldData }, standartWorldData);
            CurrentScene.Initialize();
            OnSceneInitialize?.Invoke(CurrentScene);
        }
        internal async Task HandleOpenScene()
        {
            if (CurrentScene != null && CurrentScene.IsDirty)
            {
                var res = await ConfirmationDialog.Show(
                    _mainWindow,
                    "Worning",
                    "There are scene changes. Do you want to safe scene?",
                    true);

                switch (res)
                {
                    case ConfirmationDialog.DialogResult.Cancel:
                        return;
                    case ConfirmationDialog.DialogResult.Yes:
                        var t = await FileDialogService.SaveFileAsync(
                            _mainWindow,
                            $"Safe {CurrentScene.WorldName}",
                            $"{CurrentScene.WorldName}",
                            new FileDialogService.FileFilter("scene", "scene"));

                        if (t != null) Status.SetStatus($"{t}");
                        break;

                    case ConfirmationDialog.DialogResult.No:
                        break;
                }
            }
            var loadedScene = await SceneFileHelper.OpenSceneAsync(_mainWindow);
            if (loadedScene != null)
            {
                OnScenUnload?.Invoke();
                CurrentScene = loadedScene;
                CurrentScene.Initialize();
                OnSceneInitialize?.Invoke(CurrentScene);
                Status.SetStatus($"Opened scene: {CurrentScene.WorldName}");
            }
            else
            {
                Status.SetStatus($"Opening scene failed");
            }
        }
        internal async Task HandleSaveScene()
        {
            if (string.IsNullOrEmpty(CurrentScene.ScenePath))
            {
                await HandleSaveSceneAs();
            }
            else
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                };
                OnSceneBeforeSave?.Invoke();
                string jsonContent = JsonConvert.SerializeObject(CurrentScene, jsonSettings);
                bool result = await FileDialogService.WriteTextFileAsync(CurrentScene.ScenePath, jsonContent);
                if (result)
                {
                    CurrentScene.MakeUndirty();
                }
                OnSceneAfterSave?.Invoke();
            }
        }
        internal async Task HandleSaveSceneAs()
        {
            var result = await SceneFileHelper.SaveSceneAsync(
                _mainWindow, 
                CurrentScene, 
                beforeSafe: () => OnSceneBeforeSave?.Invoke(),
                afterSafe: () => OnSceneAfterSave?.Invoke()
                );
            if (result.Item1)
            {
                CurrentScene.MakeUndirty();
                Status.SetStatus($"Scene saved: {result.Item2}");
            }
            else
            {
                Status.SetStatus("Failed to save scene");
            }
        }

    }

    internal class SceneEntityComponentProvider : IEntityComponentInfoProvider
    {
        private SceneManager _sceneManager;

        public SceneEntityComponentProvider(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public unsafe ref T GetComponent<T>(uint entityId) where T : struct, IComponent
        {
            Type type = typeof(T);
            var component = _sceneManager
                    .CurrentScene
                    .CurrentWorldData
                    .Entities
                    .First(e => e.Id == entityId)
                    .Components
                    .FirstOrDefault(e => e.Value.GetType() == type).Value;
            return ref Unsafe.Unbox<T>(component);
        }
        public bool HasComponent<T>(uint entityId) where T : struct, IComponent
        {
            Type type = typeof(T);
            var entityData = _sceneManager
                    .CurrentScene
                    .CurrentWorldData
                    .Entities
                    .FirstOrDefault(e => e.Id == entityId);
            if (entityData != null)
                return entityData.Components.Any(e => e.Value.GetType() == type);

            return false;
        }
    }
}
