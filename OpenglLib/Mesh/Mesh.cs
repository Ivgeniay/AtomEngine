using AtomEngine.RenderEntity;
using OpenglLib.Buffers;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class Mesh : MeshBase
    {
        private readonly GL _gl;
        private readonly VAO _vao;
        private readonly VBO _vbo;
        private readonly EBO? _ebo;
        private readonly uint _indicesCount;
        private readonly PrimitiveType _primitiveType;

        public Mesh(GL gl, float[] vertices, uint[] indices, PrimitiveType primitiveType = PrimitiveType.Triangles)
        {
            _gl = gl;
            _primitiveType = primitiveType;
            _indicesCount = (uint)indices.Length;
             
            _vao = new VAO(gl);
            _vbo = new VBO(gl);
            _ebo = new EBO(gl);
             
            _vbo.SetData(vertices);
            _ebo.SetData(indices);
             
            _vao.WithVBO(_vbo)
                .WithEBO(_ebo)
                .WithAttribute(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        }
         
        public Mesh(GL gl, float[] vertices, PrimitiveType primitiveType = PrimitiveType.Triangles)
        {
            _gl = gl;
            _primitiveType = primitiveType;
            _indicesCount = (uint)vertices.Length / 3; // Предполагаем, что каждая вершина имеет 3 компонента (x,y,z)

            _vao = new VAO(gl);
            _vbo = new VBO(gl);

            _vbo.SetData(vertices);

            _vao.WithVBO(_vbo)
                .WithAttribute(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        }

        public override unsafe void Draw(ShaderBase shader)
        {
            shader.Use();
            _vao.Bind();

            if (_ebo != null)
            {
                _gl.DrawElements(_primitiveType, _indicesCount, DrawElementsType.UnsignedInt, null);
            }
            else
            {
                _gl.DrawArrays(_primitiveType, 0, _indicesCount);
            }

            _vao.Unbind();
        }

        public override void Dispose()
        {
            _vao.Dispose();
            _vbo.Dispose();
            _ebo?.Dispose();
        }
    }
}
