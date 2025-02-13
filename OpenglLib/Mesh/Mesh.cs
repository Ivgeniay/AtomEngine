using AtomEngine.RenderEntity;
using AtomEngine;
using EngineLib;
using OpenglLib.Buffers;
using Silk.NET.OpenGL; 

namespace OpenglLib
{
    public class Mesh : MeshBase
    {
        public IReadOnlyList<Texture> Textures { get; private set; }
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }

        public Mesh(GL gl, float[] vertices, uint[] indices, List<Texture> textures)
        {
            if (vertices.Length % 8 != 0)
                throw new ArgumentError("Vertices array length must be multiple of 8");
            if (indices.Length % 3 != 0)
                throw new ArgumentError("Indices array length must be multiple of 3");

            GL = gl;
            Vertices = vertices;
            Indices = indices;
            Textures = textures;

            int vertexCount = vertices.Length / 8;
            Vertices_ = new Vertex[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                int index = i * 8;
                Vertices_[i] = new Vertex()
                {
                    Position = new System.Numerics.Vector3(vertices[index], vertices[index + 1], vertices[index + 2]),
                    Normal = new System.Numerics.Vector3(vertices[index + 3], vertices[index + 4], vertices[index + 5]),
                    TexCoords = new System.Numerics.Vector2(vertices[index + 6], vertices[index + 7]),
                    Index = i,
                };
            }

            Triangles = new Triangle[indices.Length/3];
            int iteration = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Triangle triangle = new Triangle(
                    Vertices_[indices[i]].Position,
                    Vertices_[indices[i + 1]].Position,
                    Vertices_[indices[i + 2]].Position
                );
                Triangles[iteration++] = triangle;
            }

            SetupMesh();
        }

        private unsafe void SetupMesh()
        {
            EBO = new BufferObject<uint>(GL, Indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(GL, Vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(GL, VBO, EBO);
            VAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
            VAO.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 8, 3);
            VAO.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 8, 6);
        }

        public void Bind()
        {
            VAO.Bind();
        }

        public override void Dispose()
        {
            if (Textures != null && Textures.Count > 0)
            {
                foreach (var texture in Textures)
                {
                    texture.Dispose();
                }
            }
            Textures = null;
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }

        public unsafe override void Draw(ShaderBase shader)
        {
            shader.Use();
            //GL.DrawArrays(Silk.NET.OpenGL.PrimitiveType.Triangles, 0, (uint)Vertices.Length);
            GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
        }
    }
}
