#vertex
#version 330 core 

layout(location = 0) in vec3 vertex_position;
layout(location = 1) in vec3 vertex_color;
layout(location = 2) in vec2 vertex_texcoord;
layout(location = 3) in vec3 vertex_normal;
uniform mat4 model_position_m4;
uniform mat4 model_rotation_m4;
uniform mat4 model_scale_m4;

uniform vec3 model_position;
uniform vec3 model_rotation;
uniform vec3 model_scale;

uniform mat4 view;
uniform mat4 projection;

uniform float TIME;
uniform float delta_time;
uniform vec2 screen_resolution;

uniform bool isBool;
uniform int num1
uniform uint unum;
uniform float fnum;
uniform double dnum;
uniform ivec2 ivector2;
uniform ivec3 ivector3;
uniform ivec4 ivector4;
uniform uvec2 uvector2;
uniform uvec3 uvector3;
uniform uvec4 uvector4;
uniform vec2 vector2;
uniform vec3 vector3;
uniform vec4 vector4;
uniform dvec2 doubleVector2;
uniform dvec3 doubleVector3;
uniform dvec4 doubleVector4;
uniform mat2 matrix2;
uniform mat3 matrix3;
uniform mat4 matrix4;
uniform mat2x2 matrix2x2;
uniform mat2x3 matrix2x3;
uniform mat2x4 matrix2x4;
uniform mat3x2 matrix3x2;
uniform mat3x3 matrix3x3;
uniform mat3x4 matrix3x4;
uniform mat4x2 matrix4x2;
uniform mat4x3 matrix4x3;
uniform mat4x4 matrix4x4;
uniform dmat2 doubleMatrix2;
uniform dmat3 doubleMatrix3;
uniform dmat4 doubleMatrix4;
uniform dmat2x2 doubleMatrix2x2;
uniform dmat2x3 doubleMatrix2x3;
uniform dmat2x4 doubleMatrix2x4;
uniform dmat3x2 doubleMatrix3x2;
uniform dmat3x3 doubleMatrix3x3;
uniform dmat3x4 doubleMatrix3x4;
uniform dmat4x2 doubleMatrix4x2;
uniform dmat4x3 doubleMatrix4x3;
uniform dmat4x4 doubleMatrix4x4;

uniform DirectionalLight light;
uniform DirectionalLight light2;
uniform DirectionalLight light33;

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

uniform mat4 model;

void main()
{
    gl_Position = projection * view * model * vec4(vertex_position.xyz, 1.0);
    vt_out.col = vec3(1.0, 1.0, 1.0);
    vt_out.norm = vec3(1.0, 1.0, 1.0);
    vt_out.frag_pos = fragmentPosition(model, vertex_position);
    vt_out.frag_norm = fragmentNormal(model, vertex_normal);
}

#fragment
#version 330 core

struct DirectionalLight {
    vec3 direction;
    vec3 color;
    float ambient_strength;
    float intensity;
};

struct PointLight {
    vec3 position;
    vec3 color;
    float ambient_strength;
    float intensity;
    float constant;
    float linear;
    float quadratic;
};


layout(std140) uniform LightingData {
    DirectionalLight directionalLights[4];
    PointLight pointLights[8];
    int dirLightCount;
    int pointLightCount;
};
vec3 calculateDirectionalLight(DirectionalLight light, vec3 baseColor, vec3 normal) {
    vec3 normalizedNormal = normalize(normal);
    vec3 normalizedLightDir = normalize(-light.direction);

    vec3 ambient = light.ambient_strength * baseColor;
    float diff = max(dot(normalizedNormal, normalizedLightDir), 0.0);
    vec3 diffuse = diff * baseColor * light.color * light.intensity;

    return ambient + diffuse;
}

vec3 calculatePointLight(PointLight light, vec3 baseColor, vec3 normal, vec3 fragPos) {
    vec3 normalizedNormal = normalize(normal);
    vec3 lightDir = light.position - fragPos;
    float distance = length(lightDir);
    lightDir = normalize(lightDir);

    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);
    attenuation = max(attenuation, 0.0001);

    vec3 ambient = light.ambient_strength * baseColor;
    float diff = max(dot(normalizedNormal, lightDir), 0.0);
    vec3 diffuse = diff * baseColor * light.color * light.intensity;

    return (ambient + diffuse) * attenuation;
}

vec3 calculateLighting(vec3 baseColor, vec3 normal, vec3 fragPos) {
    vec3 result = vec3(0.0);

    for (int i = 0; i < 4; i++) {
        if (length(directionalLights[i].color) < 0.0001 || directionalLights[i].intensity < 0.0001) {
            continue;
        }
        float isActive = float(i < dirLightCount);
        result += calculateDirectionalLight(directionalLights[i], baseColor, normal) * isActive;
    }

    for (int i = 0; i < 8; i++) {
        if (length(pointLights[i].color) < 0.0001 || pointLights[i].intensity < 0.0001) {
            continue;
        }
        float isActive = float(i < pointLightCount);
        result += calculatePointLight(pointLights[i], baseColor, normal, fragPos) * isActive;
    }

    if (length(result) < 0.0001) return baseColor;
    return result;
}





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