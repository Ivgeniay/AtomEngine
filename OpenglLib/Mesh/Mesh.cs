using AtomEngine.RenderEntity;
using OpenglLib.Buffers;
using Silk.NET.OpenGL; 

namespace OpenglLib
{
    public class Mesh : MeshBase
    {
        public float[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }
        public IReadOnlyList<Texture> Textures { get; private set; }
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }

        public Mesh(GL gl, float[] vertices, uint[] indices, List<Texture> textures)
        {
            GL = gl;
            Vertices = vertices;
            Indices = indices;
            Textures = textures;
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
