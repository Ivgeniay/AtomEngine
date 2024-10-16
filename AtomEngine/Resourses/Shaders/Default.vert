#version 330 core
layout (location = 0) in vec3 aPosition; // vertex position
layout (location = 1) in vec2 aTexCoord; // texture coordinates

out vec2 texCoord; // texture coordinates

void main()
{
	gl_Position = vec4(aPosition, 1.0);
	texCoord = aTexCoord;
}