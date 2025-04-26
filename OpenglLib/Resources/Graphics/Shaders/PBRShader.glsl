#vertex
#version 420 core

#include "embedded:Resources/Graphics/RS/Const.rs"
#include "embedded:Resources/Graphics/RS/VertexAttributes.rs"
#include "embedded:Resources/Graphics/RS/View.rs"
#include "embedded:Resources/Graphics/RS/Camera.rs"


vec4 TransformToHClip(vec3 position, mat4 model)
{
    Camera activeCamera = cameraData.cameras[cameraData.activeCameraIndex];
    return activeCamera.projectionMatrix * activeCamera.viewMatrix * model * vec4(position, 1.0);
}

uniform bool calculateViewDirPerPixel;

out VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoord;
    vec3 Tangent;
    vec3 Bitangent;
    vec4 Color;
    vec3 ViewDir;
} vs_out;

void main() {
	mat4 model = createModelMatrix();
	Camera activeCamera = cameraData.cameras[cameraData.activeCameraIndex];
	
    vs_out.FragPos = vec3(model * vec4(aPosition, 1.0));
    vs_out.Normal = mat3(transpose(inverse(model))) * aNormal;
    vs_out.TexCoord = aTexCoord;
    vs_out.Tangent = mat3(model) * aTangent;
    vs_out.Bitangent = mat3(model) * aBitangent;
    vs_out.Color = aColor;
    
    vs_out.ViewDir = normalize(activeCamera.position - vs_out.FragPos);
    gl_Position = TransformToHClip(aPosition, model);
}


#fragment
#version 420 core
#include "embedded:Resources/Graphics/RS/Pbr_Utils.rs"

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoord;
    vec3 Tangent;
    vec3 Bitangent;
    vec4 Color;
    vec3 ViewDir;
} fs_in;

layout(location = 0) out vec4 FragColor;

void main() {

    vec3 color = calculatePBR(
        fs_in.FragPos,
        fs_in.Normal,
        fs_in.Tangent,
        fs_in.Bitangent,
        fs_in.TexCoord,
        fs_in.ViewDir
    );
    
	//float depthValue = texture(shadowMapsArray, vec3(fs_in.TexCoord, 0)).r;
    //depthValue = 1.0 - (1.0 - depthValue) * 25.0;
    //FragColor = vec4(depthValue);

    FragColor = vec4(color, getAlpha());
}