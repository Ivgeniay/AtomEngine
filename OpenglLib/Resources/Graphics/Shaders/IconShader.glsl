#vertex
#version 420 core
#include "embedded:Resources/Graphics/RS/VertexAttributes.rs"
#include "embedded:Resources/Graphics/RS/View.rs"
#include "embedded:Resources/Graphics/RS/Camera.rs"

uniform float iconSize = 1.0;
uniform vec3 iconRotation = vec3(0.0, 0.0, 0.0);

out VS_OUT {
    vec2 TexCoord;
    vec4 Color;
} vs_out;

mat3 rotateX(float angle) {
    float s = sin(radians(angle));
    float c = cos(radians(angle));
    return mat3(
        1.0, 0.0, 0.0,
        0.0,   c,  -s,
        0.0,   s,   c
    );
}

mat3 rotateY(float angle) {
    float s = sin(radians(angle));
    float c = cos(radians(angle));
    return mat3(
          c, 0.0,   s,
        0.0, 1.0, 0.0,
         -s, 0.0,   c
    );
}

mat3 rotateZ(float angle) {
    float s = sin(radians(angle));
    float c = cos(radians(angle));
    return mat3(
          c,  -s, 0.0,
          s,   c, 0.0,
        0.0, 0.0, 1.0
    );
}

void main() {
    Camera activeCamera = cameraData.cameras[cameraData.activeCameraIndex];
    
    vec4 clipPos = activeCamera.projectionMatrix * activeCamera.viewMatrix * vec4(modelPosition, 1.0);
    vec3 ndcPos = clipPos.xyz / clipPos.w;
    
    vec3 vertexPos = aPosition;
    vertexPos = rotateX(iconRotation.x) * vertexPos;
    vertexPos = rotateY(iconRotation.y) * vertexPos;
    vertexPos = rotateZ(iconRotation.z) * vertexPos;
    
    float scale = iconSize / 100.0;
    vec2 offset = vertexPos.xy * scale;
    offset.x /= activeCamera.aspectRatio;
    gl_Position = vec4(ndcPos.xy + offset, ndcPos.z, 1.0);
    vs_out.TexCoord = aTexCoord;
    vs_out.Color = aColor;
}

#fragment
#version 420 core
in VS_OUT {
    vec2 TexCoord;
    vec4 Color;
} fs_in;

uniform sampler2D iconTexture;

layout(location = 0) out vec4 FragColor;

void main() {
    vec4 texColor = texture(iconTexture, fs_in.TexCoord);
    if(texColor.a < 0.01)
        discard;
    FragColor = texColor * fs_in.Color;
}