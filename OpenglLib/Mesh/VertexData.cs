using System.Numerics;

namespace OpenglLib
{
    public struct VertexData
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector3 Tangent { get; set; }
        public Vector3 Bitangent { get; set; }
        public Vector2 TexCoords { get; set; }
        public Vector4 Color { get; set; } = Vector4.One;
        public int[] BoneIds { get; set; }
        public float[] Weights { get; set; }

        public const int MAX_BONE_INFLUENCE = 4;

        public VertexData()
        {
            Position = Vector3.Zero;
            Normal = Vector3.Zero;
            Tangent = Vector3.Zero;
            Bitangent = Vector3.Zero;
            TexCoords = Vector2.Zero;
            Color = Vector4.One;
            BoneIds = new int[MAX_BONE_INFLUENCE];
            Weights = new float[MAX_BONE_INFLUENCE];
        }
    }

}
