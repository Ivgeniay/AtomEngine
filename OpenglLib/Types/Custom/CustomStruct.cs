using AtomEngine;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    /*
     CustomStruct является родительским объектом всех кастомных пользовательских структуру генерируемых из glsl кода.
    Реализует систему Dirty, которая устанавливает флаг Dirty для себя и кастомных структур которые аггрегирует.
     */
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
