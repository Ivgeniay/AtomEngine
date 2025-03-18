using System.Collections.Generic;
using Silk.NET.OpenGL;
using OpenglLib;
using System;

using Texture = OpenglLib.Texture;
using System.Threading.Tasks;
using AtomEngine.RenderEntity;

namespace Editor
{
    public class EditorRuntimeResourceManager : IService, IDisposable
    {
        private Dictionary<(string, object), object> _resourceCache = new Dictionary<(string, object), object>();
        private Dictionary<object, string> _objectToGuidCache = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

        private GL _gl;
        private bool _isGLInitialized = false;

        private MetadataManager _metadataManager;
        private TextureFactory _textureFactory;
        private MaterialFactory _materialFactory;
        private MeshFactory _meshFactory;

        public Task InitializeAsync()
        {
            GLController.OnGLInitialized += OnGLInitialized;
            GLController.OnGLDeInitialized += Dispose;

            _metadataManager = ServiceHub.Get<MetadataManager>();
            _textureFactory = ServiceHub.Get<TextureFactory>();
            _materialFactory = ServiceHub.Get<MaterialFactory>();
            _meshFactory = ServiceHub.Get<MeshFactory>();

            return Task.CompletedTask;
        }

        private void OnGLInitialized(GL gl)
        {
            _gl = gl;
            _isGLInitialized = true;

            //ReloadGLResources();
        }

        public T GetResource<T>(string guid, object context = null) where T : class
        {
            return GetResource(guid, context) as T;
        }

        public object GetResource(string guid, object context = null)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            if (_resourceCache.TryGetValue((guid, context), out var cachedResource))
                return cachedResource;

            var resource = LoadResourceByGuid(guid, context);

            if (resource != null)
            {
                _resourceCache[(guid, context)] = resource;
                _objectToGuidCache[resource] = guid;
            }

            return resource;
        }

        private object LoadResourceByGuid(string guid, object context = null)
        {
            var meta = _metadataManager.GetMetadataByGuid(guid);
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
            else if (meta.AssetType == MetadataType.Model)
            {
                return LoadMeshResource(guid, context);
            }

            return null;
        }

        private Texture LoadTextureResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var texture = _textureFactory.CreateTextureFromGuid(_gl, guid);
            return texture;
        }

        private ShaderBase LoadMaterialResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var material = _materialFactory.CreateMaterialInstanceFromGuid(_gl, guid);
            return material ;
        }

        private MeshBase LoadMeshResource(string guid, object context)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var mesh = _meshFactory.CreateMeshInstanceFromGuid(_gl, guid, context);
            return mesh;
        }

 
        public void Dispose()
        {
            _textureFactory.Dispose();
            _materialFactory.Dispose();
            _meshFactory.Dispose();

            _resourceCache.Clear();
            _objectToGuidCache.Clear();
        }

    }
}
