using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class DirectionalLight : CustomStruct
    {
        public DirectionalLight(Silk.NET.OpenGL.GL gl) : base(gl) {
        }


        public int DIRLocation { get ; set; } = -1;
        private Vector3D<float> _DIR;
        public Vector3D<float> DIR
        {
            set
            {
                if (DIRLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _DIR = value;
                _gl.Uniform3(DIRLocation, value.X, value.Y, value.Z);
            }
        }


        public int COLORLocation { get ; set; } = -1;
        private Vector3D<float> _COLOR;
        public Vector3D<float> COLOR
        {
            set
            {
                if (COLORLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _COLOR = value;
                _gl.Uniform3(COLORLocation, value.X, value.Y, value.Z);
            }
        }


        public int AMB_STRLocation { get ; set; } = -1;
        private float _AMB_STR;
        public float AMB_STR
        {
            set
            {
                if (AMB_STRLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _AMB_STR = value;
                _gl.Uniform1(AMB_STRLocation, value);
            }
        }


        public int INTENSITYLocation { get ; set; } = -1;
        private float _INTENSITY;
        public float INTENSITY
        {
            set
            {
                if (INTENSITYLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _INTENSITY = value;
                _gl.Uniform1(INTENSITYLocation, value);
            }
        }


    }
}
