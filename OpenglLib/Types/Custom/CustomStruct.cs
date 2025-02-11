using AtomEngine;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public abstract class CustomStruct
    {
        protected GL _gl;
        public CustomStruct(GL gl) => this._gl = gl;

    }
}
