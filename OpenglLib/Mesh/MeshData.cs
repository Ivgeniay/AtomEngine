using System.Numerics;

namespace OpenglLib
{
    public class MeshData
    {
        public List<VertexData> Vertices { get; set; } = new List<VertexData>();
        public List<uint> Indices { get; set; } = new List<uint>();
        public List<TextureInfo> TextureInfos { get; set; } = new List<TextureInfo>();
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public Matrix4x4 Transformation { get; set; } = Matrix4x4.Identity;

        public uint[] GetIndices()
        {
            return Indices.ToArray();
        }
    }

}
