using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class Col : CustomStruct
    {
        public Col(Silk.NET.OpenGL.GL gl) : base(gl) {
            _mat = new Matg(_gl);
        }


        private Matg _mat;
        public Matg mat
        {
            get
            {
                return _mat;
            }
        }
    }
}
