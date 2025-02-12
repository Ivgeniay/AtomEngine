using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class PointLight : CustomStruct
    {
        public PointLight(Silk.NET.OpenGL.GL gl) : base(gl) {
        }


        public int POSLocation { get ; set; } = -1;
        private Vector3D<float> _POS;
        public Vector3D<float> POS
        {
            set
            {
                if (POSLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _POS = value;
                _gl.Uniform3(POSLocation, value.X, value.Y, value.Z);
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


        public int CONSTLocation { get ; set; } = -1;
        private float _CONST;
        public float CONST
        {
            set
            {
                if (CONSTLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _CONST = value;
                _gl.Uniform1(CONSTLocation, value);
            }
        }


        public int LINEARLocation { get ; set; } = -1;
        private float _LINEAR;
        public float LINEAR
        {
            set
            {
                if (LINEARLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _LINEAR = value;
                _gl.Uniform1(LINEARLocation, value);
            }
        }


        public int QUADRATICLocation { get ; set; } = -1;
        private float _QUADRATIC;
        public float QUADRATIC
        {
            set
            {
                if (QUADRATICLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _QUADRATIC = value;
                _gl.Uniform1(QUADRATICLocation, value);
            }
        }


    }
}
