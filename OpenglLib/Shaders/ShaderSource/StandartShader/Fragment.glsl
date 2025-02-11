#version 330 core



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
	vec3 bColor = f_in.col;
    FragColor = vec4(bColor, 1.0);
}