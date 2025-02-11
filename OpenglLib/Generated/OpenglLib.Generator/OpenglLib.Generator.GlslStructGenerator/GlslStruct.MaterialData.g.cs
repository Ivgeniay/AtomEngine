using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class MaterialData : CustomStruct
    {
        public MaterialData(Silk.NET.OpenGL.GL gl) : base(gl) {
        }


        public int diffuseColorLocation { get ; set; } = -1;
        private Vector3D<float> _diffuseColor;
        public Vector3D<float> diffuseColor
        {
            set
            {
                if (diffuseColorLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _diffuseColor = value;
                _gl.Uniform3(diffuseColorLocation, value.X, value.Y, value.Z);
            }
        }


        public int specularColorLocation { get ; set; } = -1;
        private Vector3D<float> _specularColor;
        public Vector3D<float> specularColor
        {
            set
            {
                if (specularColorLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _specularColor = value;
                _gl.Uniform3(specularColorLocation, value.X, value.Y, value.Z);
            }
        }


        public int shininessLocation { get ; set; } = -1;
        private float _shininess;
        public float shininess
        {
            set
            {
                if (shininessLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _shininess = value;
                _gl.Uniform1(shininessLocation, value);
            }
        }


    }
}
