using System.Collections.Generic;
using Silk.NET.OpenGL;
using OpenglLib;
using System;

using Texture = OpenglLib.Texture;
using System.Threading.Tasks;
using AtomEngine.RenderEntity;

namespace Editor
{
    public class ResourceManager : IService, IDisposable
    {
        private Dictionary<string, object> _resourceCache = new Dictionary<string, object>();
        private Dictionary<object, string> _objectToGuidCache = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

        private GL _gl;
        private bool _isGLInitialized = false;

        public Task InitializeAsync()
        {
            GLController.OnGLInitialized += OnGLInitialized;
            GLController.OnGLDeInitialized += Dispose;
            return Task.CompletedTask;
        }

        // Обработка события инициализации GL
        private void OnGLInitialized(GL gl)
        {
            _gl = gl;
            _isGLInitialized = true;

            ReloadGLResources();
        }

        public T GetResource<T>(string guid) where T : class
        {
            return GetResource(guid) as T;
        }

        // Получение ресурса по GUID
        public object GetResource(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            if (_resourceCache.TryGetValue(guid, out var cachedResource))
                return cachedResource;

            var resource = LoadResourceByGuid(guid);

            if (resource != null)
            {
                _resourceCache[guid] = resource;
                _objectToGuidCache[resource] = guid;
            }

            return resource;
        }

        // Загрузка ресурса по GUID
        private object LoadResourceByGuid(string guid)
        {
            var meta = ServiceHub.Get<MetadataManager>().GetMetadataByGuid(guid);
            if (meta == null)
                return null;

            if (meta.AssetType == MetadataType.Texture)
            {
                return LoadTextureResource(guid);
            }
            else if (meta.AssetType == MetadataType.Material)
            {
                return LoadMaterialResource(guid);
            }

            return null;
        }

        // Загрузка текстуры
        private Texture LoadTextureResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var texture = ServiceHub.Get<TextureFactory>().CreateTextureFromGuid(_gl, guid);
            return texture;
        }

        // Загрузка материала
        private ShaderBase LoadMaterialResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var material = ServiceHub.Get<MaterialFactory>().CreateMaterialInstanceFromGuid(_gl, guid);
            return material ;
        }
      

        private void ReloadGLResources()
        {
            var resourcesNeedingReload = new List<string>();

            foreach (var kvp in _resourceCache)
            {
                if (kvp.Value is Texture || kvp.Value is MaterialAsset)
                {
                    resourcesNeedingReload.Add(kvp.Key);
                }
            }

            // Перезагружаем каждый ресурс
            foreach (var guid in resourcesNeedingReload)
            {
                var oldResource = _resourceCache[guid];
                Type resourceType = oldResource.GetType();

                // Очищаем ресурс из кэша
                _resourceCache.Remove(guid);
                _objectToGuidCache.Remove(oldResource);

                // Загружаем заново
                var newResource = LoadResourceByGuid(guid);
                if (newResource != null)
                {
                    _resourceCache[guid] = newResource;
                    _objectToGuidCache[newResource] = guid;
                }
            }
        }

        // Освобождение всех ресурсов
        public void Dispose()
        {
            foreach (var resource in _resourceCache.Values)
            {
                if (resource is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            ServiceHub.Get<TextureFactory>().Dispose();
            ServiceHub.Get<MaterialFactory>().Dispose();

            _resourceCache.Clear();
            _objectToGuidCache.Clear();
        }

    }
}
