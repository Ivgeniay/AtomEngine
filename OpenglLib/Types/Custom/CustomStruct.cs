using AtomEngine;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    /*
     CustomStruct является родительским объектом всех кастомных пользовательских структуру генерируемых из glsl кода.
    Реализует Dirty Flag pattern, которая устанавливает флаг Dirty для себя и кастомных структур которые аггрегирует.
     */
    public abstract class CustomStruct : IDirty
    {
        protected readonly Mat _shader;
        protected readonly GL _gl;
        protected bool _isDirty = true;
        public virtual bool IsDirty
        {
            get => _isDirty;
            set => _isDirty = value;
        }

        public CustomStruct(GL gl, Mat shader = null)
        {
            this._gl = gl;
            this._shader = shader;
        }

        public virtual void SetClean()
        {
            _isDirty = false;
        }
    }
}
