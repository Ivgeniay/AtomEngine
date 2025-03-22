using AtomEngine.RenderEntity;
using EngineLib;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class OpenGLRuntimeResourceManager : RuntimeResourceManager
    {
        protected GL _gl;
        protected bool _isGLInitialized = false;
        protected TextureFactory _textureFactory;
        protected MeshFactory _meshFactory;

        public override Task InitializeAsync()
        {
            _textureFactory = ServiceHub.Get<TextureFactory>();
            _meshFactory = ServiceHub.Get<MeshFactory>();

            return base.InitializeAsync();
        }

        protected virtual void OnGLInitialized(GL gl)
        {
            _gl = gl;
            _isGLInitialized = true;
        }

        protected override object LoadResourceByGuid(string guid, object context = null)
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

        protected virtual Texture LoadTextureResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var texture = _textureFactory.CreateTextureFromGuid(_gl, guid);
            return texture;
        }

        protected virtual ShaderBase LoadMaterialResource(string guid)
        {
            throw new NotFiniteNumberException();
        }

        protected virtual MeshBase LoadMeshResource(string guid, object context)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var mesh = _meshFactory.CreateMeshInstanceFromGuid(_gl, guid, context);
            return mesh;
        }


        public override void Dispose()
        {
            _textureFactory.Dispose();
            _meshFactory.Dispose();

            _resourceCache.Clear();
            _objectToGuidCache.Clear();

            _isGLInitialized = false;
        }
    }
}
