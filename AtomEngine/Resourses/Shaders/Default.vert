#version 330 core
layout (location = 0) in vec3 aPosition; // vertex position
layout (location = 1) in vec2 aTexCoord; // texture coordinates

out vec2 texCoord; // texture coordinates
//uniform variables
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
	gl_Position = vec4(aPosition, 1.0) * model * view * projection;
	texCoord = aTexCoord;
}