#include "embedded:Resources/Graphics/RS/Light.rs"
const float PI = 3.14159265359;

struct PBRMaterial {
    vec3 albedo;
    float metallic;
    float roughness;
    float ao;
    float alpha;
};

layout(std140, binding = 2) uniform MaterialUBO {
    PBRMaterial material;
    bool useAlbedoMap;
    bool useNormalMap;
    bool useMetallicMap;
    bool useRoughnessMap;
    bool useAoMap;
    bool calculateViewDirPerPixel;
};

uniform sampler2D albedoMap;
uniform sampler2D normalMap;
uniform sampler2D metallicMap;
uniform sampler2D roughnessMap;
uniform sampler2D aoMap;
uniform vec3 cameraPos;

float DistributionGGX(vec3 N, vec3 H, float roughness) {
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    
    float nom = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
    
    return nom / max(denom, 0.0000001);
}

float GeometrySchlickGGX(float NdotV, float roughness) {
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;
    float nom = NdotV;
    float denom = NdotV * (1.0 - k) + k;
    
    return nom / max(denom, 0.0000001);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness) {
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);
    
    return ggx1 * ggx2;
}

vec3 FresnelSchlick(float cosTheta, vec3 F0) {
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 getNormalFromMap(vec2 texCoord, vec3 normal, vec3 tangent, vec3 bitangent) {
    vec3 tangentNormal = texture(normalMap, texCoord).rgb * 2.0 - 1.0;

    vec3 N = normalize(normal);
    vec3 T = normalize(tangent);
    vec3 B = normalize(bitangent);
    mat3 TBN = mat3(T, B, N);

    return normalize(TBN * tangentNormal);
}

vec3 calculateDirectionalLightPBR(DirectionalLight light, vec3 normal, vec3 viewDir, PBRMaterial mat) {
    if (light.enabled < 0.5)
        return vec3(0.0);
        
    vec3 lightDir = normalize(-light.direction);
    vec3 halfwayDir = normalize(viewDir + lightDir);
    
    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, mat.albedo, mat.metallic);
    
    float NDF = DistributionGGX(normal, halfwayDir, mat.roughness);
    float G = GeometrySmith(normal, viewDir, lightDir, mat.roughness);
    vec3 F = FresnelSchlick(max(dot(halfwayDir, viewDir), 0.0), F0);
    
    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - mat.metallic;
    
    vec3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0);
    vec3 specular = numerator / max(denominator, 0.0001);
    
    float NdotL = max(dot(normal, lightDir), 0.0);
    
    return (kD * mat.albedo / PI + specular) * light.color * light.intensity * NdotL;
}

vec3 calculatePointLightPBR(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, PBRMaterial mat) {
    if (light.enabled < 0.5)
        return vec3(0.0);
        
    vec3 lightDir = normalize(light.position - fragPos);
    vec3 halfwayDir = normalize(viewDir + lightDir);
    
    float attenuation = calculatePointLightAttenuation(light, fragPos);
    if (attenuation <= 0.0)
        return vec3(0.0);
    
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, mat.albedo, mat.metallic);
    
    float NDF = DistributionGGX(normal, halfwayDir, mat.roughness);
    float G = GeometrySmith(normal, viewDir, lightDir, mat.roughness);
    vec3 F = FresnelSchlick(max(dot(halfwayDir, viewDir), 0.0), F0);
    
    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - mat.metallic;
    
    vec3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0);
    vec3 specular = numerator / max(denominator, 0.0001);
    
    float NdotL = max(dot(normal, lightDir), 0.0);
    
    return (kD * mat.albedo / PI + specular) * light.color * light.intensity * attenuation * NdotL;
}

vec3 calculatePBR(vec3 fragPos, vec3 normal, vec3 tangent, vec3 bitangent, vec2 texCoord, vec3 viewDirIn) {
    vec3 albedoValue = useAlbedoMap ? texture(albedoMap, texCoord).rgb : material.albedo;
    float metallicValue = useMetallicMap ? texture(metallicMap, texCoord).r : material.metallic;
    float roughnessValue = useRoughnessMap ? texture(roughnessMap, texCoord).r : material.roughness;
    float aoValue = useAoMap ? texture(aoMap, texCoord).r : material.ao;
    
    PBRMaterial mat;
    mat.albedo = albedoValue;
    mat.metallic = metallicValue;
    mat.roughness = max(roughnessValue, 0.04);
    mat.ao = aoValue;
    mat.alpha = material.alpha;
    
    vec3 normalValue = useNormalMap ? getNormalFromMap(texCoord, normal, tangent, bitangent) : normalize(normal);
    
    vec3 viewDir;
    if (calculateViewDirPerPixel) {
        viewDir = normalize(cameraPos - fragPos);
    } else {
        viewDir = normalize(viewDirIn);
    }
    
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, mat.albedo, mat.metallic);
    
    vec3 Lo = vec3(0.0);
    
    for (int i = 0; i < MAX_DIRECTIONAL_LIGHTS; i++) {
        if (i >= lights.numDirectionalLights)
            break;
            
        DirectionalLight light = lights.directionalLights[i];
        if (light.enabled > 0.5) {
            vec3 lightContrib = calculateDirectionalLightPBR(light, normalValue, viewDir, mat);
            
            if (light.castShadows > 0.5) {
                vec4 fragPosLightSpace = light.lightSpaceMatrix * vec4(fragPos, 1.0);
                float shadow = calculateDirectionalShadow(light, fragPosLightSpace);
                lightContrib *= (1.0 - shadow);
            }
            
            Lo += lightContrib;
        }
    }
    
    for (int i = 0; i < MAX_POINT_LIGHTS; i++) {
        if (i >= lights.numPointLights)
            break;
            
        PointLight light = lights.pointLights[i];
        if (light.enabled > 0.5) {
            Lo += calculatePointLightPBR(light, normalValue, fragPos, viewDir, mat);
        }
    }
    
    vec3 ambient = lights.ambientColor * lights.ambientIntensity * mat.albedo * mat.ao;
    vec3 color = ambient + Lo;
    
    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0/2.2));
    
    return color;
}

float getAlpha() {
    return material.alpha;
}