#vertex
#version 420 core

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

uniform vec3 col;

void main()
{
    gl_Position = PROJ * VIEW * MODEL * vec4(V_POS.xyz, 1.0);
    vt_out.col = col;
    vt_out.norm = vec3(1.0f, 1.0f, 1.0f);
    vt_out.frag_pos = fragmentPosition(MODEL, V_POS);
    vt_out.frag_norm = fragmentNormal(MODEL, V_NORM);
}


#fragment
#version 420 core

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
}