using Silk.NET.Maths;

namespace OpenglLib
{
    public class NodeM
    {
        public string Name { get; set; }
        public List<Mesh> Meshes { get; set; }
        public List<NodeM> Children { get; set; }
        public Matrix4X4<float> Transform { get; set; }

        public NodeM(string name, Matrix4X4<float> transform)
        { 
            Name = name;
            Transform = transform;
            Meshes = new List<Mesh>();
            Children = new List<NodeM>();
        }
    }
}
