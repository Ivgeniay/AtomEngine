using AtomEngine;
using System;
using System.Collections.Generic;

namespace Editor
{
    internal class ReloaderAssemblyData
    {
        private Queue<ICacheble> cachebles = new Queue<ICacheble>();
        private SceneManager _sceneManager;
        private ComponentService _componentService;
        private EventHub _eventHub;

        private string _sceneCache = string.Empty;

        public ReloaderAssemblyData()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            _componentService = ServiceHub.Get<ComponentService>();
            _eventHub = ServiceHub.Get<EventHub>();

            _eventHub.Subscribe<AssemblyUnloadEvent>(Unload);
            _eventHub.Subscribe<AssemblyUploadEvent>(Upload);
        }

        private void Unload(AssemblyUnloadEvent assembly)
        {
            try
            {
                DebLogger.Info($"Начинаем выгрузку сборки: {assembly.Assembly.FullName}");
                ServiceHub.Get<EditorAssemblyManager>().FreeCache();
                ServiceHub.Get<DraggableWindowManagerService>().Unload();
                _sceneCache = SceneSerializer.SerializeScene(_sceneManager.CurrentScene);
                _sceneManager.FreeCache();
                _componentService.FreeCache();
                ServiceHub.Get<EditorRuntimeResourceManager>().Dispose();

                foreach (var cacheble in cachebles)
                {
                    cacheble.FreeCache();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                DebLogger.Info("Выгрузка сборки успешно завершена");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при выгрузке сборки: {ex.Message}");
            }
        }

        private void Upload(AssemblyUploadEvent assembly)
        {
            if (string.IsNullOrEmpty(_sceneCache)) return;

            ProjectScene scene = SceneSerializer.DeserializeScene(_sceneCache);
            _sceneManager.SetNewScene(scene);
            _componentService.RebuildUserScrAssembly();
            ServiceHub.Get<DraggableWindowManagerService>().Upload();
            _sceneCache = string.Empty;
        }

        public void RegisterCacheble(ICacheble cacheble)
        {
            cachebles.Enqueue(cacheble);
        }
    }

    internal interface ICacheble
    {
        public void FreeCache();
    }
}
