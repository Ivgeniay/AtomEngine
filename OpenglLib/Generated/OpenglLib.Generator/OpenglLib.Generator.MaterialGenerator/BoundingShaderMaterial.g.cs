using OpenglLib.Buffers;
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using AtomEngine;

namespace OpenglLib
{
    public partial class BoundingShaderMaterial : Mat
    {
        protected string VertexSource = @"#version 420 core

layout(location = 0) in vec3 V_POS;

out VT_OUT{
    vec3 col;
} vt_out;

uniform mat4 MODEL;
uniform mat4 VIEW;
uniform mat4 PROJ;

uniform vec3 col;

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.col = vec3(col.x, col.y, col.z);
}";
        protected string FragmentSource = @"#version 420 core

in VT_OUT{
    vec3 col;
} f_in;

out vec4 FragColor;

void main()
{
    FragColor = vec4(f_in.col, 1.0);
}";
        public BoundingShaderMaterial(GL gl) : base(gl)
        {
            SetUpShader(VertexSource, FragmentSource);
            SetupUniformLocations();
        }


        public int MODELLocation { get ; protected set; } = -1;
        private Matrix4X4<float> _MODEL;
        public unsafe Matrix4X4<float> MODEL
        {
            set
            {
                if (MODELLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _MODEL = value;
                var mat4 = (Matrix4X4<float>)value;
                _gl.UniformMatrix4(MODELLocation, 1, false, (float*)&mat4);
            }
        }


        public int VIEWLocation { get ; protected set; } = -1;
        private Matrix4X4<float> _VIEW;
        public unsafe Matrix4X4<float> VIEW
        {
            set
            {
                if (VIEWLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _VIEW = value;
                var mat4 = (Matrix4X4<float>)value;
                _gl.UniformMatrix4(VIEWLocation, 1, false, (float*)&mat4);
            }
        }


        public int PROJLocation { get ; protected set; } = -1;
        private Matrix4X4<float> _PROJ;
        public unsafe Matrix4X4<float> PROJ
        {
            set
            {
                if (PROJLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _PROJ = value;
                var mat4 = (Matrix4X4<float>)value;
                _gl.UniformMatrix4(PROJLocation, 1, false, (float*)&mat4);
            }
        }


        public int colLocation { get ; protected set; } = -1;
        private Vector3D<float> _col;
        public Vector3D<float> col
        {
            set
            {
                if (colLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _col = value;
                _gl.Uniform3(colLocation, value.X, value.Y, value.Z);
            }
        }


    }
}
