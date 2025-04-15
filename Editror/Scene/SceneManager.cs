using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Controls;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    internal class SceneManager : IService, ICacheble
    {
        public static SceneEntityComponentProvider EntityCompProvider { get; private set; }
            
        public Action<ProjectScene>? OnSceneInitialize;
        public Action<ProjectScene>? OnSceneDirty;
        public Action? OnSceneBeforeSave;
        public Action? OnSceneAfterSave;
        public Action? OnScenUnload;

        public Action<uint, uint, IComponent>? OnComponentAdded;
        public Action<uint, uint, IComponent>? OnComponentRemoved;
        public Action<uint, uint, IComponent, bool>? OnComponentChange;

        public Action<uint, uint, EntityChange>? OnEntityChange;
        public Action<uint, uint>? OnEntityCreated;
        public Action<uint, uint, uint>? OnEntityDuplicated;
        public Action<uint, uint>? OnEntityRemoved;
        public Action<uint, uint>? OnEntityRenamed;

        public Action<uint, string, string> OnWorldRename;
        public Action<uint, string> OnWorldRemove;
        public Action<uint, string> OnWorldCreate;
        public Action<uint, string> OnWorldSelected;

        public Action<SystemData>? OnSystemAdded;
        public Action<SystemData>? OnSystemRemoved;
        public Action<SystemData>? OnSystemUpdated;
        public Action<SystemData, SystemData>? OnSystemDependencyAdded;
        public Action<SystemData, SystemData>? OnSystemDependencyRemoved;
        public Action<SystemData, uint>? OnSystemAddedToWorld;
        public Action<SystemData, uint>? OnSystemRemovedFromWorld;
        public Action<SystemData, int>? OnSystemExecutionOrderChanged;
        public Action<List<SystemData>>? OnSystemsReordered;
        public Action<SystemCategory>? OnSystemCategoryChanged;

        internal ProjectScene CurrentScene { get => _currentScene; private set => _currentScene = value; }
        private ProjectScene _currentScene;
        private Window _mainWindow;
        public SceneManager()
        {
            EntityCompProvider = new SceneEntityComponentProvider(this);
            EventHub eventHub = ServiceHub.Get<EventHub>();
        }

        public void SetMainWindow(Window window) { 
            _mainWindow = window; 
        }

        internal void AddComponent(uint entityId, Type typeComponent)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var instance = CurrentScene.AddComponent(entityId, typeComponent);
                OnComponentAdded?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
                OnSceneDirty?.Invoke(CurrentScene);
                OnEntityChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, EntityChange.ComponentAdded);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var instance = CurrentScene.AddComponent(entityId, typeComponent);
                    OnComponentAdded?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
                    OnSceneDirty?.Invoke(CurrentScene);
                    OnEntityChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, EntityChange.ComponentAdded);
                });
            }
        }
        internal void RemoveComponent(uint entityId, Type typeComponent)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var instance = CurrentScene.RemoveComponent(entityId, typeComponent);
                OnComponentRemoved?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
                OnSceneDirty?.Invoke(CurrentScene);
                OnEntityChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, EntityChange.ComponentRemoved);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var instance = CurrentScene.RemoveComponent(entityId, typeComponent);
                    OnComponentRemoved?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, (IComponent)instance);
                    OnSceneDirty?.Invoke(CurrentScene);
                    OnEntityChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, EntityChange.ComponentRemoved);
                });
            }
        }
        internal void ComponentChange(uint entityId, IComponent component, bool withIgnoreSceneView)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                OnComponentChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, component, withIgnoreSceneView);
                OnSceneDirty?.Invoke(CurrentScene);
                OnEntityChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, EntityChange.ComponentValueChange);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    OnComponentChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, component, withIgnoreSceneView);
                    OnSceneDirty?.Invoke(CurrentScene);
                    OnEntityChange?.Invoke(CurrentScene.CurrentWorldData.WorldId, entityId, EntityChange.ComponentValueChange);
                });
            }
        }

        internal void AddEntity(string entityName)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                uint id = CurrentScene.AddEntity(entityName);
                OnEntityCreated?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    uint id = CurrentScene.AddEntity(entityName);
                    OnEntityCreated?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void AddDuplicateEntity(EntityHierarchyItem hierarchyEntity)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                uint id = CurrentScene.AddDuplicateEntity(hierarchyEntity);
                OnEntityDuplicated?.Invoke(CurrentScene.CurrentWorldData.WorldId, hierarchyEntity.Id, id);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    uint id = CurrentScene.AddDuplicateEntity(hierarchyEntity);
                    OnEntityDuplicated?.Invoke(CurrentScene.CurrentWorldData.WorldId, hierarchyEntity.Id, id);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void RemoveEntity(EntityHierarchyItem entity)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                uint id = CurrentScene.DeleteEntity(entity);
                OnEntityRemoved?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    uint id = CurrentScene.DeleteEntity(entity);
                    OnEntityRemoved?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void RenameEntity(EntityHierarchyItem entity)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                uint id = CurrentScene.RenameEntity(entity);
                OnEntityRenamed?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    uint id = CurrentScene.RenameEntity(entity);
                    OnEntityRenamed?.Invoke(CurrentScene.CurrentWorldData.WorldId, id);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }

        internal void RenameWorld((string, string) worldNameLastCurrent)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                CurrentScene.RenameWorld(worldNameLastCurrent);
                OnWorldRename?.Invoke(CurrentScene.CurrentWorldData.WorldId, worldNameLastCurrent.Item1, worldNameLastCurrent.Item2);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    CurrentScene.RenameWorld(worldNameLastCurrent);
                    OnWorldRename?.Invoke(CurrentScene.CurrentWorldData.WorldId, worldNameLastCurrent.Item1, worldNameLastCurrent.Item2);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void RemoveWorld(string worldName)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var deletedWorld = CurrentScene.RemoveWorld(worldName);
                OnWorldRemove?.Invoke(deletedWorld.WorldId, worldName);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var deletedWorld = CurrentScene.RemoveWorld(worldName);
                    OnWorldRemove?.Invoke(deletedWorld.WorldId, worldName);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void CreateWorld(string worldName)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var newWorldData = CurrentScene.CreateWorld(worldName);
                OnWorldCreate?.Invoke(newWorldData.WorldId, worldName);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var newWorldData = CurrentScene.CreateWorld(worldName);
                    OnWorldCreate?.Invoke(newWorldData.WorldId, worldName);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void SelecteWorld(string worldName)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                CurrentScene.SelecteWorld(worldName);
                OnWorldSelected?.Invoke(CurrentScene.CurrentWorldData.WorldId, CurrentScene.CurrentWorldData.WorldName);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    CurrentScene.SelecteWorld(worldName);
                    OnWorldSelected?.Invoke(CurrentScene.CurrentWorldData.WorldId, CurrentScene.CurrentWorldData.WorldName);
                });
            }
        }


        internal void AddSystem(Type systemType, SystemCategory category)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var systemData = new SystemData
                {
                    SystemFullTypeName = systemType.FullName,
                    Category = category,
                    ExecutionOrder = -1
                };

                CurrentScene.Systems.Add(systemData);
                OnSystemAdded?.Invoke(systemData);
                OnSceneDirty?.Invoke(CurrentScene);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var systemData = new SystemData
                    {
                        SystemFullTypeName = systemType.FullName,
                        Category = category,
                        ExecutionOrder = -1
                    };

                    CurrentScene.Systems.Add(systemData);
                    OnSystemAdded?.Invoke(systemData);
                    OnSceneDirty?.Invoke(CurrentScene);
                });
            }
        }
        internal void RemoveSystem(SystemData system)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                foreach (var s in CurrentScene.Systems)
                {
                    if (s.Dependencies.Any(d => d.SystemFullTypeName == system.SystemFullTypeName))
                    {
                        var dependencyToRemove = s.Dependencies.FirstOrDefault(d =>
                            d.SystemFullTypeName == system.SystemFullTypeName);

                        if (dependencyToRemove != null)
                        {
                            s.Dependencies.Remove(dependencyToRemove);
                        }
                    }
                }

                var systemToRemove = CurrentScene.Systems.FirstOrDefault(s =>
                    s.SystemFullTypeName == system.SystemFullTypeName);

                if (systemToRemove != null)
                {
                    CurrentScene.Systems.Remove(systemToRemove);
                    OnSystemRemoved?.Invoke(systemToRemove);
                    OnSceneDirty?.Invoke(CurrentScene);
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    foreach (var s in CurrentScene.Systems)
                    {
                        if (s.Dependencies.Any(d => d.SystemFullTypeName == system.SystemFullTypeName))
                        {
                            var dependencyToRemove = s.Dependencies.FirstOrDefault(d =>
                                d.SystemFullTypeName == system.SystemFullTypeName);

                            if (dependencyToRemove != null)
                            {
                                s.Dependencies.Remove(dependencyToRemove);
                            }
                        }
                    }

                    var systemToRemove = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == system.SystemFullTypeName);

                    if (systemToRemove != null)
                    {
                        CurrentScene.Systems.Remove(systemToRemove);
                        OnSystemRemoved?.Invoke(systemToRemove);
                        OnSceneDirty?.Invoke(CurrentScene);
                    }
                });
            }
        }
        internal void AddSystemDependency(SystemData system, SystemData dependency)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                    s.SystemFullTypeName == system.SystemFullTypeName);

                if (systemToUpdate != null)
                {
                    var dependencyToAdd = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == dependency.SystemFullTypeName);

                    if (dependencyToAdd != null && !systemToUpdate.Dependencies.Any(d =>
                        d.SystemFullTypeName == dependencyToAdd.SystemFullTypeName))
                    {
                        systemToUpdate.Dependencies.Add(dependencyToAdd);
                        OnSystemDependencyAdded?.Invoke(systemToUpdate, dependencyToAdd);
                        OnSceneDirty?.Invoke(CurrentScene);
                    }
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == system.SystemFullTypeName);

                    if (systemToUpdate != null)
                    {
                        var dependencyToAdd = CurrentScene.Systems.FirstOrDefault(s =>
                            s.SystemFullTypeName == dependency.SystemFullTypeName);

                        if (dependencyToAdd != null && !systemToUpdate.Dependencies.Any(d =>
                            d.SystemFullTypeName == dependencyToAdd.SystemFullTypeName))
                        {
                            systemToUpdate.Dependencies.Add(dependencyToAdd);
                            OnSystemDependencyAdded?.Invoke(systemToUpdate, dependencyToAdd);
                            OnSceneDirty?.Invoke(CurrentScene);
                        }
                    }
                });
            }
        }
        internal void RemoveSystemDependency(SystemData system, SystemData dependency)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                    s.SystemFullTypeName == system.SystemFullTypeName);

                if (systemToUpdate != null)
                {
                    var dependencyToRemove = systemToUpdate.Dependencies.FirstOrDefault(d =>
                        d.SystemFullTypeName == dependency.SystemFullTypeName);

                    if (dependencyToRemove != null)
                    {
                        systemToUpdate.Dependencies.Remove(dependencyToRemove);
                        OnSystemDependencyRemoved?.Invoke(systemToUpdate, dependency);
                        OnSceneDirty?.Invoke(CurrentScene);
                    }
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == system.SystemFullTypeName);

                    if (systemToUpdate != null)
                    {
                        var dependencyToRemove = systemToUpdate.Dependencies.FirstOrDefault(d =>
                            d.SystemFullTypeName == dependency.SystemFullTypeName);

                        if (dependencyToRemove != null)
                        {
                            systemToUpdate.Dependencies.Remove(dependencyToRemove);
                            OnSystemDependencyRemoved?.Invoke(systemToUpdate, dependency);
                            OnSceneDirty?.Invoke(CurrentScene);
                        }
                    }
                });
            }
        }
        internal void AddSystemToWorld(SystemData system, uint worldId)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                    s.SystemFullTypeName == system.SystemFullTypeName);

                if (systemToUpdate != null && !systemToUpdate.IncludInWorld.Contains(worldId))
                {
                    systemToUpdate.IncludInWorld.Add(worldId);
                    OnSystemAddedToWorld?.Invoke(systemToUpdate, worldId);
                    OnSceneDirty?.Invoke(CurrentScene);

                    var world = CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                    if (world != null)
                    {
                        Status.SetStatus($"Added system to world: {system.SystemFullTypeName} -> {world.WorldName}");
                    }
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == system.SystemFullTypeName);

                    if (systemToUpdate != null && !systemToUpdate.IncludInWorld.Contains(worldId))
                    {
                        systemToUpdate.IncludInWorld.Add(worldId);
                        OnSystemAddedToWorld?.Invoke(systemToUpdate, worldId);
                        OnSceneDirty?.Invoke(CurrentScene);

                        var world = CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                        if (world != null)
                        {
                            Status.SetStatus($"Added system to world: {system.SystemFullTypeName} -> {world.WorldName}");
                        }
                    }
                });
            }
        }
        internal void RemoveSystemFromWorld(SystemData system, uint worldId)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                    s.SystemFullTypeName == system.SystemFullTypeName);

                if (systemToUpdate != null && systemToUpdate.IncludInWorld.Contains(worldId))
                {
                    systemToUpdate.IncludInWorld.Remove(worldId);
                    OnSystemRemovedFromWorld?.Invoke(systemToUpdate, worldId);
                    OnSceneDirty?.Invoke(CurrentScene);
                    var world = CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                    if (world != null)
                    {
                        Status.SetStatus($"Removed system from world: {system.SystemFullTypeName} -> {world.WorldName}");
                    }
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == system.SystemFullTypeName);

                    if (systemToUpdate != null && systemToUpdate.IncludInWorld.Contains(worldId))
                    {
                        systemToUpdate.IncludInWorld.Remove(worldId);
                        OnSystemRemovedFromWorld?.Invoke(systemToUpdate, worldId);
                        OnSceneDirty?.Invoke(CurrentScene);
                        var world = CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                        if (world != null)
                        {
                            Status.SetStatus($"Removed system from world: {system.SystemFullTypeName} -> {world.WorldName}");
                        }
                    }
                });
            }
        }
        internal void ChangeSystemExecutionOrder(SystemData system, int newOrder)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                    s.SystemFullTypeName == system.SystemFullTypeName);

                if (systemToUpdate != null && systemToUpdate.ExecutionOrder != newOrder)
                {
                    systemToUpdate.ExecutionOrder = newOrder;
                    OnSystemExecutionOrderChanged?.Invoke(systemToUpdate, newOrder);
                    OnSceneDirty?.Invoke(CurrentScene);
                    Status.SetStatus($"Changed execution order for system: {system.SystemFullTypeName} -> {newOrder}");
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var systemToUpdate = CurrentScene.Systems.FirstOrDefault(s =>
                        s.SystemFullTypeName == system.SystemFullTypeName);

                    if (systemToUpdate != null && systemToUpdate.ExecutionOrder != newOrder)
                    {
                        systemToUpdate.ExecutionOrder = newOrder;
                        OnSystemExecutionOrderChanged?.Invoke(systemToUpdate, newOrder);
                        OnSceneDirty?.Invoke(CurrentScene);
                        Status.SetStatus($"Changed execution order for system: {system.SystemFullTypeName} -> {newOrder}");
                    }
                });
            }
        }
        internal void ReorderSystems(List<SystemData> systems)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                if (systems != null && systems.Count > 0)
                {
                    OnSystemsReordered?.Invoke(systems);
                    OnSceneDirty?.Invoke(CurrentScene);
                }
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (systems != null && systems.Count > 0)
                    {
                        OnSystemsReordered?.Invoke(systems);
                        OnSceneDirty?.Invoke(CurrentScene);
                    }
                });
            }
        }


        internal List<SystemData> GetSystems()
        {
            if (CurrentScene == null)
            {
                return new List<SystemData>();
            }

            if (CurrentScene.Systems == null)
            {
                CurrentScene.Systems = new List<SystemData>();
            }

            return CurrentScene.Systems.Select(s => s.Clone()).ToList();
        }
        internal List<SystemData> GetSystemsByCategory(SystemCategory category)
        {
            if (CurrentScene == null)
            {
                return new List<SystemData>();
            }

            if (CurrentScene.Systems == null)
            {
                CurrentScene.Systems = new List<SystemData>();
            }

            return CurrentScene.Systems
                .Where(s => s.Category == category)
                .Select(s => s.Clone())
                .ToList();
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
            WorldData standartWorldData = SceneFileHelper.CreateDefauldWorldData();
            CurrentScene = new ProjectScene(new List<WorldData>() { standartWorldData }, standartWorldData);
            CurrentScene.Systems = SceneFileHelper.CreateDefaultSystems();
            OnSceneInitialize?.Invoke(CurrentScene);
        }
        internal async Task HandleOpenScene()
        {
            LoadingManager loadingManager = ServiceHub.Get<LoadingManager>();
            await loadingManager.RunWithLoading(async (progress) =>
            {
                progress.Report((0, "Scene loading..."));
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

                //var loadedScene = await SceneFileHelper.OpenSceneAsync(_mainWindow);

                var filePath = await FileDialogService.OpenFileAsync(
                    _mainWindow,
                    "Open Scene",
                    new[]
                    {
                        new FileDialogService.FileFilter("Scene Files", "scene"),
                        new FileDialogService.FileFilter("JSON Files", "json"),
                        new FileDialogService.FileFilter("All Files", "*")
                    });
                progress.Report((10, "Scene loading..."));

                if (string.IsNullOrEmpty(filePath))
                {
                    DebLogger.Error("Не удалось прочитать содержимое файла");
                    return;
                }
                progress.Report((20, "Scene loading..."));

                var fileContent = await FileDialogService.ReadTextFileAsync(filePath);
                progress.Report((30, "Scene loading..."));
                if (string.IsNullOrEmpty(fileContent))
                {
                    return;
                }

                ProjectScene? sceneData = SceneSerializer.DeserializeScene(fileContent);
                progress.Report((40, "Scene loading..."));

                //if (sceneData != null)
                //{
                //    DebLogger.Info($"Сцена загружена: {sceneData.WorldName} ({sceneData.Worlds?.Count ?? 0} миров)");
                //    return;
                //}
                ProjectScene loadedScene = sceneData;
                progress.Report((80, "Scene loading..."));
                if (loadedScene != null)
                {

                    await Dispatcher.UIThread.InvokeAsync(() => { 
                        OnScenUnload?.Invoke();
                    });
                    CurrentScene = loadedScene;
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        OnSceneInitialize?.Invoke(CurrentScene);
                    });
                    Status.SetStatus($"Opened scene: {CurrentScene.WorldName}");
                }
                else
                {
                    Status.SetStatus($"Opening scene failed");
                }
                progress.Report((100, ""));
            
            }, "Scene loading...");
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
                 
                 
                string jsonContent = SceneSerializer.SerializeScene(CurrentScene);
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
                CurrentScene);
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

        internal void CallBeforeSave()
        {
            OnSceneBeforeSave?.Invoke();
        }
        internal void CallAfterSafe()
        {
            OnSceneAfterSave?.Invoke();
        }
        internal void EntityReordered(object? sender, EntityReorderEventArgs e) => HierarchyComponentRouter.EntityReordered(sender, e, this);

        public void FreeCache()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                OnScenUnload?.Invoke();
                if (_currentScene == null) return;

                for (int i = 0; i < _currentScene.Worlds.Count; i++)
                {
                    for (int j = 0; j < _currentScene.Worlds[i].Entities.Count; j++)
                    {
                        _currentScene.Worlds[i].Entities[j].Components.Clear();
                        _currentScene.Worlds[i].Entities[j].Components = null;
                        _currentScene.Worlds[i].Entities[j] = null;
                    }
                    _currentScene.Worlds[i] = null;
                }
                _currentScene = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }));
        }

        public void SetNewScene(ProjectScene scene)
        {
            if (scene == null)
            {
                return;
            }

            CurrentScene = scene;
            OnSceneInitialize?.Invoke(CurrentScene);
        }

        internal uint GetAndReserveId(uint worldId) => CurrentScene.GetAndReserveId(worldId);
        internal void DisposeReservedId(uint worldId) => CurrentScene?.DisposeReservedId(worldId);
        internal void DisposeAllReservedId() => CurrentScene?.DisposeAllReservedId();
    }
}
