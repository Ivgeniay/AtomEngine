using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    internal class Buffer
    {
        public uint Handle => _handle;

        protected uint _handle;
        protected GL _gl;

        internal Buffer(GL gL)
        {
            _gl = gL;
        }
    }
}
