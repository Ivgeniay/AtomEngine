using AtomEngine.RenderEntity;
using EngineLib;
using OpenglLib;
using Texture = OpenglLib.Texture;

namespace WindowsBuild
{
    public class RuntimeResourceManager : IService
    {
        private readonly Dictionary<string, Texture> _textures = new();
        private readonly Dictionary<string, MeshBase> _meshes = new();
        private readonly Dictionary<string, ShaderBase> _materials = new();
        //private readonly Dictionary<string, Model> _modelCache = new Dictionary<string, Model>();
        private readonly Dictionary<string, ModelData> _modelCache = new Dictionary<string, ModelData>();

        public int TextureCount => _textures.Count;
        public int MeshCount => _meshes.Count;
        public int MaterialCount => _materials.Count;

        public void RegisterTexture(string guid, Texture texture)
        {
            _textures[guid] = texture;
        }

        public void RegisterModel(string guid, ModelData model)
        {
            _modelCache[guid] = model;
        }
        //public void RegisterModel(string guid, Model model)
        //{
        //    _modelCache[guid] = model;
        //}

        public void RegisterMesh(string guid, MeshBase mesh)
        {
            _meshes[guid] = mesh;
        }

        public void RegisterMaterial(string guid, ShaderBase material)
        {
            _materials[guid] = material;
        }

        public ModelData GetModel(string guid)
        {
            return _modelCache.TryGetValue(guid, out var model) ? model : null;
        }
        //public Model GetModel(string guid)
        //{
        //    return _modelCache.TryGetValue(guid, out var model) ? model : null;
        //}

        public Texture GetTexture(string guid)
        {
            return _textures.TryGetValue(guid, out var texture) ? texture : null;
        }

        public MeshBase GetMesh(string guid)
        {
            return _meshes.TryGetValue(guid, out var mesh) ? mesh : null;
        }

        public ShaderBase GetMaterial(string guid)
        {
            return _materials.TryGetValue(guid, out var material) ? material : null;
        }

        public void Dispose()
        {
            foreach (var texture in _textures.Values)
            {
                texture.Dispose();
            }
            _textures.Clear();

            foreach (var mesh in _meshes.Values)
            {
                mesh.Dispose();
            }
            _meshes.Clear();

            foreach (var material in _materials.Values)
            {
                material.Dispose();
            }
            _materials.Clear();
        }

        public Task InitializeAsync() => Task.CompletedTask;
    }
}
