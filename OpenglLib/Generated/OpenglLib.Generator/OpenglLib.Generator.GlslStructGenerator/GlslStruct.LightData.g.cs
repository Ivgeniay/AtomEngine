using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class LightData : CustomStruct
    {
        public LightData(Silk.NET.OpenGL.GL gl) : base(gl) {
        }


        public int positionLocation { get ; set; } = -1;
        private Vector3D<float> _position;
        public Vector3D<float> position
        {
            set
            {
                if (positionLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _position = value;
                _gl.Uniform3(positionLocation, value.X, value.Y, value.Z);
            }
        }


        public int directionLocation { get ; set; } = -1;
        private Vector3D<float> _direction;
        public Vector3D<float> direction
        {
            set
            {
                if (directionLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _direction = value;
                _gl.Uniform3(directionLocation, value.X, value.Y, value.Z);
            }
        }


        public int colorLocation { get ; set; } = -1;
        private Vector3D<float> _color;
        public Vector3D<float> color
        {
            set
            {
                if (colorLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _color = value;
                _gl.Uniform3(colorLocation, value.X, value.Y, value.Z);
            }
        }


        public int intensityLocation { get ; set; } = -1;
        private float _intensity;
        public float intensity
        {
            set
            {
                if (intensityLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _intensity = value;
                _gl.Uniform1(intensityLocation, value);
            }
        }


    }
}
