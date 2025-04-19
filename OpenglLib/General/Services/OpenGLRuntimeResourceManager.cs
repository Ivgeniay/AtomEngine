using AtomEngine;
using AtomEngine.RenderEntity;
using EngineLib;
using OpenglLib.Buffers;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class OpenGLRuntimeResourceManager : RuntimeResourceManager
    {
        protected GL _gl;
        protected bool _isGLInitialized = false;
        protected TextureFactory _textureFactory;
        protected MeshFactory _meshFactory;
        protected MaterialFactory _materialFactory;
        protected UboService _uboService;
        protected FBOService _fboService;

        public override Task InitializeAsync()
        {
            _textureFactory = ServiceHub.Get<TextureFactory>();
            _meshFactory = ServiceHub.Get<MeshFactory>();
            _materialFactory = ServiceHub.Get<MaterialFactory>();
            _uboService = ServiceHub.Get<UboService>();
            _fboService = ServiceHub.Get<FBOService>();

            return base.InitializeAsync();
        }

        protected virtual void OnGLInitialized(GL gl)
        {
            _gl = gl;
            _uboService.SetGL(gl);
            _fboService.SetGL(gl);
            _isGLInitialized = true;
        }

        protected override object LoadResourceByGuid(string guid, object context = null)
        {
            var meta = _metadataManager.GetMetadataByGuid(guid);
            if (meta == null)
                return null;

            if (meta.AssetType == MetadataType.Texture)
            {
                return LoadTextureResource(guid, context);
            }
            else if (meta.AssetType == MetadataType.Material)
            {
                return LoadMaterialResource(guid);
            }
            else if (meta.AssetType == MetadataType.Model)
            {
                return LoadMeshResource(guid, (int)context);
            }
            else if (meta.AssetType == MetadataType.Shader)
            {
                return LoadShaderResource(guid, context);
            }

            return null;
        }

        public virtual Texture LoadTextureResource(string guid, object context = null)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            try
            {
                if (context == null) throw new NullReferenceError(nameof(context));
                uint shaderProgram = Convert.ToUInt32(context);
                var texture = _textureFactory.CreateTextureFromGuid(_gl, guid, shaderProgram);
                return texture;
            }
            catch (Exception e)
            {
#if DEBUG
                DebLogger.Error($"Creation texture error {e.Message}");
#endif
                return null;
            }
        }

        public virtual Material LoadMaterialResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var material = _materialFactory.GetMaterialInstanceFromAssetGuid(_gl, guid);
            return material;
        }

        public virtual ShaderBase LoadShaderResource(string guid, object context)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var shader = _materialFactory.GetShaderFormMaterialAssetGUID(_gl, guid);
            return shader;
        }

        public virtual MeshBase LoadMeshResource(string guid, int index, Shader shader = null)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var mesh = _meshFactory.CreateMeshInstanceFromGuid(_gl, guid, index, shader);
            return mesh;
        }


        public override void Dispose()
        {
            if (_isGLInitialized)
            {
                _textureFactory.Dispose();
                _meshFactory.Dispose();
                _materialFactory.Dispose();
                _uboService.Dispose();
                _fboService.Dispose();

                _resourceCache.Clear();
                _objectToGuidCache.Clear();

                _isGLInitialized = false;
            }
        }
    }

    public class ShaderFactory : IService
    {
        protected List<ShaderData> _shaderInstanceCache = new List<ShaderData>();
        protected AssemblyManager _assemblyManager;

        public virtual Task InitializeAsync()
        {
            _assemblyManager = ServiceHub.Get<AssemblyManager>();
            return Task.CompletedTask;
        }

        //public ShaderBase GetShaderBySAID(string SAID, string entityId)
        //{
            
        //}

        public virtual void Dispose() { 
            
        }


        protected class ShaderData
        {
            public ShaderBase Shader;
            public string EntityGuid;
            public ScriptMetadata Metadata;
        }
    }
}
