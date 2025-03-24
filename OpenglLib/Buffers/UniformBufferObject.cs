using Silk.NET.OpenGL;
using EngineLib;

namespace OpenglLib.Buffers
{ 
    public class UniformBufferObject<TDataType> : IDisposable where TDataType : unmanaged
    {
        private uint _handle;
        private GL _gl;
        private uint? _bindingPoint;
        private uint _program;

        public unsafe UniformBufferObject(GL gl, ref TDataType data, uint program, uint bindingPoint)
        {
            _gl = gl;
            _bindingPoint = bindingPoint;
            _program = program;

            _handle = _gl.GenBuffer();
            Bind();

            fixed (void* d = &data)
            {
                _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)sizeof(TDataType), d, BufferUsageARB.DynamicDraw);
            }
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, bindingPoint, _handle);

            var bindingService = ServiceHub.Get<BindingPointService>();
            bindingService.AllocateBindingPoint(_program, bindingPoint);
        }



        public unsafe UniformBufferObject(GL gl, ref TDataType data, uint blockIndex)
        {
            _gl = gl;
            _handle = _gl.GenBuffer();

            if (blockIndex != uint.MaxValue)
            {
                var bindingService = ServiceHub.Get<BindingPointService>();
                _bindingPoint = bindingService.AllocateBindingPoint(_program);
                _gl.UniformBlockBinding(_program, blockIndex, _bindingPoint.Value);
            }
            Bind();

            fixed (void* d = &data)
            {
                _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)sizeof(TDataType), d, BufferUsageARB.DynamicDraw);
            }

            if (_bindingPoint.HasValue)
            {
                _gl.BindBufferBase(BufferTargetARB.UniformBuffer, _bindingPoint.Value, _handle);
            }

            //_gl.BindBufferBase(BufferTargetARB.UniformBuffer, blockIndex, _handle);
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

        public void Unbind()
        {
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
            if (_bindingPoint.HasValue)
            {
                var bindingService = ServiceHub.Get<BindingPointService>();
                bindingService.ReleaseBindingPoint(_program, _bindingPoint.Value);
            }
        }
    }
}