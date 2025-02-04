using OpenglLib.Buffers;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglLib
{
    public class Mesh : IDisposable
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

            // Создаем буферы
            _vao = new VAO(gl);
            _vbo = new VBO(gl);
            _ebo = new EBO(gl);

            // Загружаем данные
            _vbo.SetData(vertices);
            _ebo.SetData(indices);

            // Настраиваем VAO с атрибутами по умолчанию для 3D позиции
            _vao.WithVBO(_vbo)
                .WithEBO(_ebo)
                .WithAttribute(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        }

        // Конструктор для меша без индексов
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

        public unsafe void Draw(Shader shader)
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

        public void Dispose()
        {
            _vao.Dispose();
            _vbo.Dispose();
            _ebo?.Dispose();
        }
    }
}
