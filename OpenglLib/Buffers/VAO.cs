using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    internal class VAO : Buffer
    {
        private readonly List<VBO> _vbos = new(); 
        private readonly List<EBO> _ebos = new();

        public VAO(GL gl) : base(gl)
        { 
            _handle = _gl.GenVertexArray();
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void Unbind()
        {
            _gl.BindVertexArray(0);
        }

        public VAO WithVBO(VBO vbo)
        {
            Bind();
            vbo.Bind();
            _vbos.Add(vbo);
            return this;
        }

        public VAO WithEBO(EBO ebo)
        {
            Bind();
            ebo.Bind(); 
            _ebos.Add(ebo);
            return this;
        }

        public unsafe VAO WithAttribute(uint index, int size, VertexAttribPointerType type, bool normalized, uint stride, int offset)
        {
            Bind();
            _gl.VertexAttribPointer(index, size, type, normalized, stride, (void*)offset);
            _gl.EnableVertexAttribArray(index);
            return this;
        }

        public unsafe VAO WithInstanceAttribute(uint index, int size, VertexAttribPointerType type, bool normalized, uint stride, int offset, uint divisor)
        {
            Bind();
            _gl.VertexAttribPointer(index, size, type, normalized, stride, (void*)offset);
            _gl.VertexAttribDivisor(index, divisor); 
            _gl.EnableVertexAttribArray(index);
            return this;
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
            foreach (var vbo in _vbos)
            {
                vbo.Dispose();
            }
            foreach (var ebo in _ebos)
            {
                ebo.Dispose();
            } 
        }
    }
}
