using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using AtomEngine;
using System;

namespace Editor
{
    internal class SceneManager : IService
    {
        public Action<ProjectScene>? OnSceneInitialize;
        public Action<ProjectScene>? OnSceneChange;
        public Action? OnSceneBeforeSave;
        public Action? OnSceneAfterSave;
        public Action? OnScenUnload;
        public Action<uint, uint, IComponent>? OnComponentAdded;
        public Action<uint, uint, IComponent>? OnComponentRemoved;
        public Action<uint, uint, IComponent>? OnComponentChange;
        public Action<uint, uint>? OnEntityCreated;
        public Action<uint, uint>? OnEntityRemoved;

        private ProjectScene _currentScene;
        private Window _mainWindow; 

        public void SetMainWindow(Window window) { 
            _mainWindow = window; 
        }
        public void InitializeStandartScene()
        {
            WorldData standartWorldData = SceneFileHelper.CreateWorldData();
            _currentScene = new ProjectScene(
                new List<WorldData>() { standartWorldData },
                standartWorldData);
        }
        internal ProjectScene CurrentScene { get =>  _currentScene; }

        internal void AddComponent(uint entityId, Type typeComponent)
        {
            var instance = _currentScene.AddComponent(entityId, typeComponent);
            OnComponentAdded?.Invoke(_currentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
        }
        internal void RemoveComponent(uint entityId, Type typeComponent)
        {
            var instance = _currentScene.RemoveComponent(entityId, typeComponent);
            OnComponentRemoved?.Invoke(_currentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
        }
        internal void ComponentChange(uint entityId, IComponent component)
        {
            OnComponentChange?.Invoke(_currentScene.CurrentWorldData.WorldId, entityId, component);
        }

        internal void AddEntity(string entityName)
        {
            uint id = _currentScene.AddEntity(entityName);
            OnEntityCreated?.Invoke(_currentScene.CurrentWorldData.WorldId, id);
        }
        internal void AddDuplicateEntity(EntityHierarchyItem hierarchyEntity)
        {
            _currentScene.AddDuplicateEntity(hierarchyEntity);
            OnSceneChange?.Invoke(_currentScene);
        }
        internal void RenameEntity(EntityHierarchyItem entity)
        {
            uint id = _currentScene.RenameEntity(entity);
            OnEntityCreated?.Invoke(_currentScene.CurrentWorldData.WorldId, id);
        }
        internal void DeleteEntity(EntityHierarchyItem entity)
        {
            _currentScene.DeleteEntity(entity);
            OnSceneChange?.Invoke(_currentScene);
        }
        internal void RenameWorld((string, string) worldNameLastCurrent)
        {
            _currentScene.RenameWorld(worldNameLastCurrent);
            OnSceneChange?.Invoke(_currentScene);
        }
        internal void RemoveWorld(string worldName)
        {
            _currentScene.RemoveWorld(worldName);
            OnSceneChange?.Invoke(_currentScene);
        }
        internal void CreateWorld(string worldName)
        {
            _currentScene.CreateWorld(worldName);
            OnSceneChange?.Invoke(_currentScene);
        }
        internal void SelecteWorld(string worldName)
        {
            _currentScene.SelecteWorld(worldName);
            OnSceneChange?.Invoke(_currentScene);
        }



        public Task InitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// Обрабатывает создание новой сцены
        /// </summary>
        internal async Task HandleNewScene()
        {
            if (_currentScene != null && _currentScene.IsDirty)
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
                            $"Safe {_currentScene.WorldName}",
                            $"{_currentScene.WorldName}",
                            new FileDialogService.FileFilter("scene", "scene"));

                        if (t != null) Status.SetStatus($"{t}");
                        break;

                    case ConfirmationDialog.DialogResult.No:
                        break;
                }
            }

            OnScenUnload?.Invoke();
            WorldData standartWorldData = SceneFileHelper.CreateWorldData();
            _currentScene = new ProjectScene(new List<WorldData>() { standartWorldData }, standartWorldData);
            OnSceneInitialize?.Invoke(CurrentScene);
        }

        /// <summary>
        /// Обрабатывает открытие сцены
        /// </summary>
        internal async Task HandleOpenScene()
        {
            if (_currentScene != null && _currentScene.IsDirty)
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
                            $"Safe {_currentScene.WorldName}",
                            $"{_currentScene.WorldName}",
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
                _currentScene = loadedScene;
                OnSceneInitialize?.Invoke(CurrentScene);
                Status.SetStatus($"Opened scene: {_currentScene.WorldName}");
            }
            else
            {
                Status.SetStatus($"Opening scene failed");
            }
        }

        /// <summary>
        /// Обрабатывает сохранение текущей сцены
        /// </summary>
        internal async Task HandleSaveScene()
        {
            if (string.IsNullOrEmpty(_currentScene.ScenePath))
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
                string jsonContent = JsonConvert.SerializeObject(_currentScene, jsonSettings);
                bool result = await FileDialogService.WriteTextFileAsync(_currentScene.ScenePath, jsonContent);
                if (result)
                {
                    _currentScene.MakeUndirty();
                }
                OnSceneAfterSave?.Invoke();
            }
        }

        /// <summary>
        /// Обрабатывает сохранение сцены с выбором имени файла
        /// </summary>
        internal async Task HandleSaveSceneAs()
        {
            var result = await SceneFileHelper.SaveSceneAsync(
                _mainWindow, 
                _currentScene, 
                beforeSafe: () => OnSceneBeforeSave?.Invoke(),
                afterSafe: () => OnSceneAfterSave?.Invoke()
                );
            if (result.Item1)
            {
                _currentScene.MakeUndirty();
                Status.SetStatus($"Scene saved: {result.Item2}");
            }
            else
            {
                Status.SetStatus("Failed to save scene");
            }
        }
    }
}
