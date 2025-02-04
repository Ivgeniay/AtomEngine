using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    internal class VBO : Buffer
    {
        private readonly BufferTargetARB _bufferType;

        public VBO(GL gl, BufferTargetARB bufferType = BufferTargetARB.ArrayBuffer) : base(gl)
        {
            _bufferType = bufferType;
            _handle = _gl.GenBuffer();
        }

        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle);
        }

        public void Unbind()
        {
            _gl.BindBuffer(_bufferType, 0);
        }

        public unsafe void SetData<T>(T[] data, BufferUsageARB usage = BufferUsageARB.StaticDraw) where T : unmanaged
        {
            Bind();
            fixed (void* d = &data[0])
            {
                _gl.BufferData(_bufferType, (nuint)(data.Length * sizeof(T)), d, usage);
            }
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}
