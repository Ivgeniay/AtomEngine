using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class SceneData : CustomStruct
    {
        public SceneData(Silk.NET.OpenGL.GL gl) : base(gl) {
            directionData  = new LocaleArray<Vector3D<float>>(3, _gl);
        }


        public int directionDataLocation
        {
             get => directionData.Location;
             set => directionData.Location = value;
        }
        public LocaleArray<Vector3D<float>> directionData;


        public int ambientColorLocation { get ; set; } = -1;
        private Vector3D<float> _ambientColor;
        public Vector3D<float> ambientColor
        {
            set
            {
                if (ambientColorLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _ambientColor = value;
                _gl.Uniform3(ambientColorLocation, value.X, value.Y, value.Z);
            }
        }


    }
}
