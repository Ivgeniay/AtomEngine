#vertex
#version 420 core

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
}


#fragment
#version 420 core

in VT_OUT{
    vec3 col;
} f_in;

out vec4 FragColor;

void main()
{
    FragColor = vec4(f_in.col, 1.0);
}