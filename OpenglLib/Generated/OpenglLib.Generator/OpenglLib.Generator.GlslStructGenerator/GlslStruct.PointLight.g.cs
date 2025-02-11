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


        public int ambient_strengthLocation { get ; set; } = -1;
        private float _ambient_strength;
        public float ambient_strength
        {
            set
            {
                if (ambient_strengthLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _ambient_strength = value;
                _gl.Uniform1(ambient_strengthLocation, value);
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


        public int constantLocation { get ; set; } = -1;
        private float _constant;
        public float constant
        {
            set
            {
                if (constantLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _constant = value;
                _gl.Uniform1(constantLocation, value);
            }
        }


        public int linearLocation { get ; set; } = -1;
        private float _linear;
        public float linear
        {
            set
            {
                if (linearLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _linear = value;
                _gl.Uniform1(linearLocation, value);
            }
        }


        public int quadraticLocation { get ; set; } = -1;
        private float _quadratic;
        public float quadratic
        {
            set
            {
                if (quadraticLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _quadratic = value;
                _gl.Uniform1(quadraticLocation, value);
            }
        }


    }
}
