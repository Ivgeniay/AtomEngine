#version 330 core

#include "BaseParts/VertexAttributes.glsl"
#include "BaseParts/VertexUniforms.glsl"
#include "BaseParts/VertexFuncs.glsl"

out VT_OUT{
    vec2 uv;
    vec3 col;
    vec3 norm;
    vec3 frag_pos;
    vec3 frag_norm;
} vt_out;

uniform mat4 MODEL;

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.col = vec3(1.0, 1.0, 1.0);
    vt_out.norm = vec3(1.0, 1.0, 1.0);
    vt_out.frag_pos = fragmentPosition(model, V_POS);
    vt_out.frag_norm = fragmentNormal(model, V_NORM);
}