#vertex
#version 330 core

#include "embedded:Resources/Graphics/RS/VertexAttributes.rs"
#include "embedded:Resources/Graphics/RS/View.rs"

uniform mat4 lightSpaceMatrix;

void main()
{
    mat4 model = createModelMatrix();
    gl_Position = lightSpaceMatrix * model * vec4(aPosition, 1.0);
}

#fragment
#version 330 core

void main()
{
}