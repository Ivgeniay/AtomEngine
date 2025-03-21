using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglLib
{
    public class PBRShader : Mat
    {
        protected new string VertexSource = @"#version 330 core

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

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform vec3 col;

void main()
{
    gl_Position = projection * view * model * vec4(V_POS.xyz, 1.0);
    vt_out.uv = V_UV;
    vt_out.col = col;
    vt_out.norm = V_NORM;
    vt_out.frag_pos = fragmentPosition(model, V_POS);
    vt_out.frag_norm = fragmentNormal(model, V_NORM);
}";
        protected new string FragmentSource = @"#version 330 core

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
    FragColor = texColor *  vec4(f_in.col, 1.0);
}";
        public PBRShader(GL gl) : base(gl)
        {
            SetUpShader(VertexSource, FragmentSource);
            SetupUniformLocations();
        }
    }
}
