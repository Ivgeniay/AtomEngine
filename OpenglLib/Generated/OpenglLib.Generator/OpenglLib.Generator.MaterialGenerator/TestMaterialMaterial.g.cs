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

struct Kek {
	float ambient;
	float c[3];
};

struct Matg {
	float ambient;
    Kek kok[3];
    float c[3];
};

struct Col {
	Matg mat;
	Matg matArray[2];
};

uniform float normals[3];
uniform Col coloring;
uniform Col coloringArray[2];

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.col = vec3(  coloringArray[1].mat.c[1] + coloringArray[0].mat.kok[2].c[1] + coloring.mat.c[0] + normals[0],
                        coloringArray[1].mat.c[1] + coloringArray[1].mat.ambient + coloring.mat.c[1] + normals[1],
                        coloringArray[1].mat.c[2] + coloring.mat.c[2] + normals[2]);
    vt_out.norm = vec3(coloring.mat.c[0], coloring.mat.c[1], coloring.mat.c[2]);
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
            _normals  = new LocaleArray<float>(3, _gl);
            _coloring = new Col(_gl);
            _coloringArray  = new StructArray<Col>(2, _gl);
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


        public int normalsLocation
        {
             get => normals.Location;
             set => normals.Location = value;
        }
        private LocaleArray<float> _normals;
        public LocaleArray<float> normals
        {
            get
            {
                return _normals;
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


        private StructArray<Col> _coloringArray;
        public StructArray<Col> coloringArray
        {
            get
            {
                return _coloringArray;
            }
        }


    }
}
