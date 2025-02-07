
#version 330 core
layout(location = 0) in vec3 aPosition;

uniform mat4 transform;

void main()
{
    gl_Position = transform * vec4(aPosition, 1.0);
}