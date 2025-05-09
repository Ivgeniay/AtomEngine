﻿#include "embedded:Resources/Graphics/RS/Camera.rs"

const int MAX_DIRECTIONAL_LIGHTS = 4;
const int MAX_POINT_LIGHTS = 8;
const int MAX_SPOT_LIGHTS = 8;
const int MAX_CASCADES = 4;

struct CascadeData {
    mat4 lightSpaceMatrix;
    float splitDepth;
};

struct DirectionalLight {
    vec3 direction;
    vec3 color;
    float intensity;
    float castShadows;
    CascadeData cascades[MAX_CASCADES];
    float enabled;
    int lightId;
    int numCascades;
};

struct PointLight {
    vec3 position;
    vec3 color;
    float intensity;
    float radius;
    float castShadows;
    float falloffExponent;
    float enabled;
    int lightId;
};

struct SpotLight {
    vec3 position;
    vec3 direction;
    vec3 color;
    float intensity;
    float innerCutoff;    
    float outerCutoff;    
    float radius;         
    float castShadows;
    mat4 lightSpaceMatrix;
    float enabled;
    int lightId;
};

layout(std140, binding = 1) uniform LightsUBO {
    DirectionalLight directionalLights[MAX_DIRECTIONAL_LIGHTS];
    PointLight pointLights[MAX_POINT_LIGHTS];
    SpotLight spotLights[MAX_SPOT_LIGHTS];
    vec3 ambientColor;
    float ambientIntensity;
    int numDirectionalLights;
    int numPointLights;
    int numSpotLights;
    float shadowBias;
    int pcfKernelSize;
    float shadowIntensity;
} lights;

layout(binding = 10) uniform sampler2DArray shadowMapsArray;
layout(binding = 11) uniform samplerCubeArray pointShadowMapsArray;

float calculatePointLightAttenuation(PointLight light, vec3 fragPos) {
    float distance = length(light.position - fragPos);
    if (distance > light.radius)
        return 0.0;
    float normalizedDistance = distance / light.radius;
    float attenuation = pow(1.0 - normalizedDistance, light.falloffExponent);
    
    return attenuation;
}


float calculateDirectionalShadowWithAdaptivePCF(DirectionalLight dirLight, vec4 fragPosLightSpace, int lightIndex, vec3 viewPos, vec3 fragPos) {

    if (dirLight.castShadows < 0.5 || dirLight.enabled < 0.5)
        return 0.0;
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    
    if (projCoords.z > 1.0 || projCoords.x < 0.0 || projCoords.x > 1.0 || 
        projCoords.y < 0.0 || projCoords.y > 1.0)
        return 0.0;
    
    if (lightIndex < 0 || lightIndex >= lights.numDirectionalLights)
        return 0.0;
    
    float currentDepth = projCoords.z;
    float bias = lights.shadowBias;
    
    vec2 texelSize = 1.0 / vec2(textureSize(shadowMapsArray, 0));
    
    int kernelSize = lights.pcfKernelSize;
    float distance = length(viewPos - fragPos);
    float maxDistance = 100.0;
    int adaptiveKernel = int(min(kernelSize, max(1, kernelSize * (1.0 - distance / maxDistance))));

    float totalSamples = (2.0 * adaptiveKernel + 1.0) * (2.0 * adaptiveKernel + 1.0);
    float invTotalSamples = 1.0 / totalSamples;
    
    
    float shadow = 0.0;
    
    for (int x = -adaptiveKernel; x <= adaptiveKernel; ++x) {
        for (int y = -adaptiveKernel; y <= adaptiveKernel; ++y) {
            vec2 sampleCoord = projCoords.xy + vec2(x, y) * texelSize;
            float pcfDepth = texture(shadowMapsArray, vec3(sampleCoord, lightIndex)).r;
            shadow += (currentDepth - bias > pcfDepth) ? 1.0 : 0.0;
        }
    }
    shadow *= invTotalSamples * lights.shadowIntensity;
    
    return shadow;
}


float calculateCascadedDirectionalShadow(DirectionalLight light, vec3 fragPos, vec3 viewPos) {
    if (light.castShadows < 0.5 || light.enabled < 0.5 || light.numCascades == 0)
        return 0.0;
    
    float viewDistance = length(fragPos - viewPos);
    float viewDepth = viewDistance / cameraData.cameras[cameraData.activeCameraIndex].farPlane;
    
    int cascadeIndex = 0;
    for (int i = 0; i < light.numCascades - 1; i++) {
        if (viewDepth < light.cascades[i].splitDepth) {
            cascadeIndex = i;
            break;
        }
        cascadeIndex = i + 1;
    }
    
    vec4 fragPosLightSpace = light.cascades[cascadeIndex].lightSpaceMatrix * vec4(fragPos, 1.0);
    
    int lightIndex = -1;
    for (int i = 0; i < lights.numDirectionalLights; i++) {
        if (lights.directionalLights[i].lightId == light.lightId) {
            lightIndex = i;
            break;
        }
    }
    
    if (lightIndex < 0)
        return 0.0;
    
    // Индекс слоя текстуры для текущего каскада
    int layerIndex = lightIndex * MAX_CASCADES + cascadeIndex;
    
    // Расчет теней для текущего каскада
    float shadow = calculateDirectionalShadowWithAdaptivePCF(
        light, fragPosLightSpace, layerIndex, viewPos, fragPos);
    
    // Плавный переход между каскадами
    if (cascadeIndex < light.numCascades - 1) {
        float nextSplit = light.cascades[cascadeIndex].splitDepth;
        float blendThreshold = 0.1 * nextSplit;
        float blendZone = nextSplit - blendThreshold;
        
        if (viewDepth > blendZone) {
            // Расчет теней для следующего каскада
            vec4 fragPosLightSpaceNext = light.cascades[cascadeIndex + 1].lightSpaceMatrix * vec4(fragPos, 1.0);
            int nextLayerIndex = lightIndex * MAX_CASCADES + (cascadeIndex + 1);
            
            float nextShadow = calculateDirectionalShadowWithAdaptivePCF(
                light, fragPosLightSpaceNext, nextLayerIndex, viewPos, fragPos);
            
            // Плавное смешивание между текущим и следующим каскадами
            float blendFactor = (viewDepth - blendZone) / blendThreshold;
            shadow = mix(shadow, nextShadow, blendFactor);
        }
    }
    
    return shadow;
}



float calculatePointShadow(PointLight light, vec3 fragPos, int lightIndex) {
    if (light.castShadows < 0.5 || light.enabled < 0.5)
        return 0.0;
    
    if (lightIndex < 0 || lightIndex >= lights.numPointLights)
        return 0.0;
    
    vec3 lightToFrag = fragPos - light.position;
    float distance = length(lightToFrag);
    
    if (distance > light.radius)
        return 0.0;
    
    vec3 direction = normalize(lightToFrag);
    
    float currentDepth = distance;
    float bias = lights.shadowBias;

    float texelSize = 1.0 / float(textureSize(pointShadowMapsArray, 0).x);
    int kernelSize = lights.pcfKernelSize;
    
    float maxDistance = light.radius;
    //F = 1/(Kc + Kd + kd^2)
    int adaptiveKernel = int(min(kernelSize, max(1, kernelSize * (1.0 - distance / maxDistance))));
    
    float totalSamples = (2.0 * adaptiveKernel + 1.0) * (2.0 * adaptiveKernel + 1.0);
    float invTotalSamples = 1.0 / totalSamples;
    
    float shadow = 0.0;
    
    vec3 tangent = vec3(1.0, 0.0, 0.0);
    if (abs(direction.x) > 0.99) tangent = vec3(0.0, 1.0, 0.0);
    tangent = normalize(cross(direction, tangent));
    vec3 bitangent = normalize(cross(direction, tangent));
    
    for (int x = -adaptiveKernel; x <= adaptiveKernel; ++x) {
        for (int y = -adaptiveKernel; y <= adaptiveKernel; ++y) {
            vec3 offset = tangent * (float(x) * texelSize) + bitangent * (float(y) * texelSize);
            offset *= 0.02;
            vec3 sampleDir = normalize(direction + offset);
            
            float closestDepth = texture(pointShadowMapsArray, vec4(sampleDir, lightIndex)).r;
            
            closestDepth *= light.radius; 
            
            shadow += (currentDepth - bias > closestDepth) ? 1.0 : 0.0;
        }
    }
    
    shadow = shadow * invTotalSamples * lights.shadowIntensity;
    float fadeStart = light.radius * 0.85;
    if (distance > fadeStart) {
        float fadeLength = light.radius - fadeStart;
        float fadeProgress = (distance - fadeStart) / fadeLength;
        shadow *= 1.0 - fadeProgress;
    }
    
    return shadow;
}