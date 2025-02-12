#version 330 core

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

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.col = vec3(1.0, 1.0, 1.0);
    vt_out.norm = vec3(1.0, 1.0, 1.0);
    vt_out.frag_pos = fragmentPosition(model, V_POS);
    vt_out.frag_norm = fragmentNormal(model, V_NORM);
}