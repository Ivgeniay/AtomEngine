using AtomEngine;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public abstract class CustomStruct : IDirty
    {
        protected bool _isDirty = true;
        public virtual bool IsDirty
        {
            get => _isDirty;
            set => _isDirty = value;
        }

        protected GL _gl;
        public CustomStruct(GL gl) => this._gl = gl;

        public virtual void SetClean()
        {
            _isDirty = false;
        }
    }
}
