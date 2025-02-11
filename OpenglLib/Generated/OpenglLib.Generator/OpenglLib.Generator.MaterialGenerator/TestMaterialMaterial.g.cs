using Silk.NET.OpenGL;
using Silk.NET.Maths;
using AtomEngine;

namespace OpenglLib
{
    public class TestMaterialMaterial : Mat
    {
        protected string VertexSource = @"#version 330 core

layout(location = 0) in vec3 V_POS;
layout(location = 1) in vec3 V_COL;
layout(location = 2) in vec2 V_UV;
layout(location = 3) in vec3 V_NORM;

vec3 fragmentNormal(mat4 modelMatrix, vec3 vertexNormal) {
    return mat3(modelMatrix) * vertexNormal;
}

vec3 fragmentPosition(mat4 modelMatrix, vec3 vertexPosition) {
    return (modelMatrix * vec4(vertexPosition, 1.0)).xyz;
}

out VT_OUT{
    vec2 uv;
    vec3 col;
    vec3 norm;
    vec3 frag_pos;
    vec3 frag_norm;
} vt_out;

uniform mat4 MODEL;
uniform mat4 VIEW;
uniform mat4 PROJ;

struct Matg {
	float ambient; 
    float c[3];
};;

struct Col {
	Matg mat;
};

uniform Col coloring;

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.col = vec3(coloring.mat.ambient, coloring.mat.c[1], coloring.mat.c[2]);
    vt_out.norm = vec3(1.0, 1.0, 1.0);
    vt_out.frag_pos = fragmentPosition(MODEL, V_POS);
    vt_out.frag_norm = fragmentNormal(MODEL, V_NORM);
}";
        protected string FragmentSource = @"#version 330 core

in VT_OUT{
    vec2 uv;
    vec3 col;
    vec3 norm;
    vec3 frag_pos;
    vec3 frag_norm;
} f_in;

out vec4 FragColor;

void main()
{ 
    FragColor = vec4(f_in.col, 1.0);
}";
        public TestMaterialMaterial(GL gl) : base(gl)
        {
            _coloring = new Col(_gl);
            SetUpShader(VertexSource, FragmentSource);
            SetLocation();
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


        private Col _coloring;
        public Col coloring
        {
            get
            {
                return _coloring;
            }
        }


    }
}
