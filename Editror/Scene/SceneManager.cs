using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using System;

namespace Editor
{
    internal class SceneManager : IService
    {
        public Action<ProjectScene> OnSceneInitialize;

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

        public void AddComponent(uint entityId, Type typeComponent)
        {
            _currentScene.AddComponent(entityId, typeComponent);
        }

        public void RemoveComponent(uint entityId, Type typeComponent)
        {
            _currentScene.RemoveComponent(entityId, typeComponent);
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

                string jsonContent = JsonConvert.SerializeObject(_currentScene, jsonSettings);
                bool result = await FileDialogService.WriteTextFileAsync(_currentScene.ScenePath, jsonContent);
                if (result)
                {
                    _currentScene.MakeUndirty();
                    Status.SetStatus($"Save scene succesful");
                }
                else Status.SetStatus($"Save scene not succesful");
            }
        }

        /// <summary>
        /// Обрабатывает сохранение сцены с выбором имени файла
        /// </summary>
        internal async Task HandleSaveSceneAs()
        {
            var result = await SceneFileHelper.SaveSceneAsync(_mainWindow, _currentScene);
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
