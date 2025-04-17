const int MAX_DIRECTIONAL_LIGHTS = 4;
const int MAX_POINT_LIGHTS = 8;

struct DirectionalLight {
    vec3 direction;
    vec3 color;
    float intensity;
    float castShadows;
    mat4 lightSpaceMatrix;
    float enabled;
    int lightId;
};

struct PointLight {
    vec3 position;
    vec3 color;
    float intensity;
    float radius;
    float castShadows;
    float falloffExponent;
    float enabled;
};

layout(std140, binding = 1) uniform LightsUBO {
    DirectionalLight directionalLights[MAX_DIRECTIONAL_LIGHTS];
    PointLight pointLights[MAX_POINT_LIGHTS];
    vec3 ambientColor;
    float ambientIntensity;
    int numDirectionalLights;
    int numPointLights;
    float shadowBias;
    int pcfKernelSize;
    float shadowIntensity;
} lights;

uniform sampler2DArray shadowMapsArray;

float calculatePointLightAttenuation(PointLight light, vec3 fragPos) {
    float distance = length(light.position - fragPos);
    if (distance > light.radius)
        return 0.0;
    float normalizedDistance = distance / light.radius;
    float attenuation = pow(1.0 - normalizedDistance, light.falloffExponent);
    
    return attenuation;
}

float calculateDirectionalShadow(DirectionalLight dirLight, vec4 fragPosLightSpace) {
    if (dirLight.castShadows < 0.5 || dirLight.enabled < 0.5)
        return 0.0;
    
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    
    if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 || 
        projCoords.y < 0.0 || projCoords.y > 1.0)
        return 0.0;
    
    int lightIndex = -1;
    for (int i = 0; i < lights.numDirectionalLights; i++) {
        if (lights.directionalLights[i].lightId == dirLight.lightId) {
            lightIndex = i;
            break;
        }
    }
    
    if (lightIndex == -1)
        return 0.0;
    
    float currentDepth = projCoords.z;
    float bias = lights.shadowBias;
    float shadow = 0.0;
    
    int kernelSize = lights.pcfKernelSize;
    vec2 texelSize = 1.0 / vec2(textureSize(shadowMapsArray, 0));
    
    for (int x = -kernelSize; x <= kernelSize; ++x) {
        for (int y = -kernelSize; y <= kernelSize; ++y) {
            float pcfDepth = texture(shadowMapsArray, 
                                vec3(projCoords.xy + vec2(x, y) * texelSize, lightIndex)).r;
            shadow += (currentDepth - bias > pcfDepth) ? 1.0 : 0.0;
        }
    }
    
    float totalSamples = (2.0 * kernelSize + 1.0) * (2.0 * kernelSize + 1.0);
    shadow /= totalSamples;
    shadow *= lights.shadowIntensity;
    
    return shadow;
}

/*
float calculatePointLightAttenuation(PointLight light, vec3 fragPos) {
    float distance = length(light.position - fragPos);
    if (distance > light.radius)
        return 0.0;
    float normalizedDistance = distance / light.radius;
    float attenuation = pow(1.0 - normalizedDistance, light.falloffExponent);
    
    return attenuation;
}

float calculateDirectionalShadow(DirectionalLight dirLight, vec4 fragPosLightSpace) {
    if (dirLight.castShadows < 0.5 || dirLight.enabled < 0.5)
        return 0.0;
    
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    
    if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 || 
        projCoords.y < 0.0 || projCoords.y > 1.0)
        return 0.0;
    
    int lightIndex = -1;
    for (int i = 0; i < lights.numDirectionalLights; i++) {
        if (lights.directionalLights[i].lightId == dirLight.lightId) {
            lightIndex = i;
            break;
        }
    }
    
    if (lightIndex == -1)
        return 0.0;
    
    float currentDepth = projCoords.z;
    float bias = lights.shadowBias;
    float shadow = 0.0;
    
    int kernelSize = lights.pcfKernelSize;
    vec2 texelSize = 1.0 / textureSize(directionalShadowMaps[lightIndex], 0);
    
    for (int x = -kernelSize; x <= kernelSize; ++x) {
        for (int y = -kernelSize; y <= kernelSize; ++y) {
            float pcfDepth = texture(directionalShadowMaps[lightIndex], 
                                projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += (currentDepth - bias > pcfDepth) ? 1.0 : 0.0;
        }
    }
    
    float totalSamples = (2.0 * kernelSize + 1.0) * (2.0 * kernelSize + 1.0);
    shadow /= totalSamples;
    shadow *= lights.shadowIntensity;
    
    return shadow;
}
*/