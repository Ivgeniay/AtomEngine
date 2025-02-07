#version 330 core

#include "BaseParts/LightUniform.glsl"
#include "BaseParts/LightFunctions.glsl"

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
    //vec3 bColor = texture(SAM_TEXTURE00, f_in.uv).rgb;
	vec3 bColor = f_in.col;
    vec3 temp = calculateLighting(bColor, f_in.norm, f_in.frag_pos);
    // vec3 temp = calculateDirectionalLight(DL_NAME[0], bColor, f_in.norm);
    temp = temp * f_in.col;
    FragColor = vec4(temp, 1.0);
}