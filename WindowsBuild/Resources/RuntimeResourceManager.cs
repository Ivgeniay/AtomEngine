using AtomEngine.RenderEntity;
using Texture = OpenglLib.Texture;

namespace WindowsBuild
{
    public class RuntimeResourceManager
    {
        private readonly Dictionary<string, Texture> _textures = new();
        private readonly Dictionary<string, MeshBase> _meshes = new();
        private readonly Dictionary<string, ShaderBase> _materials = new();

        public int TextureCount => _textures.Count;
        public int MeshCount => _meshes.Count;
        public int MaterialCount => _materials.Count;

        public void RegisterTexture(string guid, Texture texture)
        {
            _textures[guid] = texture;
        }

        public void RegisterMesh(string guid, MeshBase mesh)
        {
            _meshes[guid] = mesh;
        }

        public void RegisterMaterial(string guid, ShaderBase material)
        {
            _materials[guid] = material;
        }

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
    }
}
