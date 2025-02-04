using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    internal class EBO : Buffer
    {
        public uint Handle => _handle;

        public EBO(GL gl) : base(gl)
        { 
            _handle = _gl.GenBuffer();
        }

        public void Bind()
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _handle);
        }

        public void Unbind()
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        public unsafe void SetData(uint[] indices, BufferUsageARB usage = BufferUsageARB.StaticDraw)
        {
            Bind();
            fixed (void* i = &indices[0])
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), i, usage);
            }
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}
