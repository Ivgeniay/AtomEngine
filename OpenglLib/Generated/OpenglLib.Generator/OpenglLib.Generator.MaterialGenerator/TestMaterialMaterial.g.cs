using OpenglLib.Buffers;
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using AtomEngine;

namespace OpenglLib
{
    public partial class TestMaterialMaterial : Mat
    {
        protected string VertexSource = @"#version 420 core

layout(location = 0) in vec3 V_POS;
layout(location = 1) in vec3 V_NORM;
layout(location = 2) in vec2 V_UV;

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

layout(std140, binding = 55) uniform CameraData
{
    mat4 view;
    mat4 projection;
    vec3 cameraPos;
	float padding;
} kek;

uniform mat4 MODEL;
uniform mat4 VIEW;
uniform mat4 PROJ;

uniform vec3 col;

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.uv = V_UV;
    vt_out.col = vec3(col.x, col.y, col.z - kek.padding);
    vt_out.norm = V_NORM;
    vt_out.frag_pos = fragmentPosition(MODEL, V_POS);
    vt_out.frag_norm = fragmentNormal(MODEL, V_NORM);
}";
        protected string FragmentSource = @"#version 420 core

in VT_OUT{
    vec2 uv;
    vec3 col;
    vec3 norm;
    vec3 frag_pos;
    vec3 frag_norm;
} f_in;

out vec4 FragColor;
uniform sampler2D tex;

void main()
{ 
    vec4 texColor = texture(tex, f_in.uv);
    FragColor = texColor * vec4(f_in.col, 1.0);
    FragColor = vec4(f_in.uv, 1.0, 1.0);
}";
        public TestMaterialMaterial(GL gl) : base(gl)
        {
            kekUbo = new UniformBufferObject<CameraData_TestMaterial>(_gl, ref _kek, handle, 55);
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


        public void tex_SetTexture(OpenglLib.Texture texture) => SetTexture("Texture0", "Texture2D", texLocation, 0, texture);
        public int texLocation { get ; protected set; } = -1;
        private int _tex;
        public int tex
        {
            set
            {
                if (texLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _tex = value;
                _gl.Uniform1(texLocation, value);
            }
        }


        private UniformBufferObject<CameraData_TestMaterial> kekUbo;
        private CameraData_TestMaterial _kek = new CameraData_TestMaterial();
        public CameraData_TestMaterial kek
        {
            set
            {
                _kek = value;
                kekUbo.UpdateData(ref _kek);
            }
        }


    }
}
