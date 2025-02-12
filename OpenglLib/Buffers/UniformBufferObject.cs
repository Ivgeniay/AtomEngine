using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{ 
    public class UniformBufferObject<TDataType> : IDisposable where TDataType : unmanaged
    {
        private uint _handle;
        private GL _gl;
        private uint? _bindingPoint;
        private uint _program;

        public unsafe UniformBufferObject(GL gl, ref TDataType data, uint bindingPoint)
        {
            _gl = gl;
            _bindingPoint = bindingPoint;

            // Создаем буфер
            _handle = _gl.GenBuffer();
            Bind();

            // Загружаем данные
            fixed (void* d = &data)
            {
                _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)sizeof(TDataType), d, BufferUsageARB.DynamicDraw);
            }

            // Привязываем к binding point
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, bindingPoint, _handle);
        }

        public unsafe UniformBufferObject(GL gl, ref TDataType data, uint program, string blockName)
        {
            _gl = gl;
            _program = program;

            _handle = _gl.GenBuffer();
            Bind();

            fixed (void* d = &data)
            {
                _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)sizeof(TDataType), d, BufferUsageARB.DynamicDraw);
            }

            uint blockIndex = _gl.GetUniformBlockIndex(_program, blockName);
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, blockIndex, _handle);
        }

        public unsafe void UpdateData(ref TDataType data)
        {
            Bind();
            fixed (void* d = &data)
            {
                _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)sizeof(TDataType), d);
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, _handle);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}