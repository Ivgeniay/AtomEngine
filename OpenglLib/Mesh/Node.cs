using Silk.NET.Maths;

namespace OpenglLib
{
    public class Node
    {
        public string Name { get; set; }
        public List<Mesh> Meshes { get; set; }
        public List<Node> Children { get; set; }
        public Matrix4X4<float> Transform { get; set; }

        public Node(string name, Matrix4X4<float> transform)
        { 
            Name = name;
            Transform = transform;
            Meshes = new List<Mesh>();
            Children = new List<Node>();
        }
    }
}
