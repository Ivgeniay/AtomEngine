using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class Matg : CustomStruct
    {
        public Matg(Silk.NET.OpenGL.GL gl) : base(gl) {
            c  = new LocaleArray<float>(3, _gl);
        }


        public int ambientLocation { get ; set; } = -1;
        private float _ambient;
        public float ambient
        {
            set
            {
                if (ambientLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _ambient = value;
                _gl.Uniform1(ambientLocation, value);
            }
        }


        public int cLocation
        {
             get => c.Location;
             set => c.Location = value;
        }
        public LocaleArray<float> c;


    }
}
