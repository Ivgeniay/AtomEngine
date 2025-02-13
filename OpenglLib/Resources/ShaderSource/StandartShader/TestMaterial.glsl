#vertex
#version 420 core

layout(location = 0) in vec3 V_POS;
layout(location = 1) in vec2 V_UV;
layout(location = 2) in vec3 V_NORM;

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
uniform sampler2D tex;

void main()
{ 
    vec4 texColor = texture(tex, f_in.uv);
    FragColor = texColor * vec4(f_in.col, 1.0);
    FragColor = vec4(f_in.uv, 1.0, 1.0);
}