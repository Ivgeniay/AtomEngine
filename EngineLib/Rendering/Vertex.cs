using System.Numerics;

namespace AtomEngine
{
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoords;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public int Index;

        public const int MAX_BONE_INFLUENCE = 4;
        public int[] BoneIds;
        public float[] Weights;

        public static explicit operator Vertex(float[] vertices)
        {
            return new Vertex()
            {
                Position = new Vector3(vertices[0], vertices[1], vertices[2]),
                Normal = new Vector3(vertices[3], vertices[4], vertices[5]),
                TexCoords = new Vector2(vertices[6], vertices[7]),
            };
        }

        public static explicit operator Vector3(Vertex vertex)
        {
            return vertex.Position;
        }
    }
}
