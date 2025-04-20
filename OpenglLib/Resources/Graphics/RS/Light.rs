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

layout(binding = 10) uniform sampler2DArray shadowMapsArray;
uniform samplerCubeArray pointShadowMapsArray;

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
            float pcfDepth = texture(shadowMapsArray, vec3(projCoords.xy + vec2(x, y) * texelSize, lightIndex)).r;
            shadow += (currentDepth - bias > pcfDepth) ? 1.0 : 0.0;
        }
    }
    
    float totalSamples = (2.0 * kernelSize + 1.0) * (2.0 * kernelSize + 1.0);
    shadow /= totalSamples;
    shadow *= lights.shadowIntensity;
    
    return shadow;
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
    int adaptiveKernel = int(min(kernelSize, max(1, kernelSize * (1.0 - distance / maxDistance))));
    
    float totalSamples = (2.0 * adaptiveKernel + 1.0) * (2.0 * adaptiveKernel + 1.0);
    float invTotalSamples = 1.0 / totalSamples;
    
    float shadow = 0.0;
    
    // Генерируем базис для семплирования в пространстве вокруг направления
    vec3 tangent = vec3(1.0, 0.0, 0.0);
    if (abs(direction.x) > 0.99) tangent = vec3(0.0, 1.0, 0.0);
    tangent = normalize(cross(direction, tangent));
    vec3 bitangent = normalize(cross(direction, tangent));
    
    // PCF для точечного источника света
    for (int x = -adaptiveKernel; x <= adaptiveKernel; ++x) {
        for (int y = -adaptiveKernel; y <= adaptiveKernel; ++y) {
            // Создаем смещенное направление для PCF
            vec3 offset = tangent * (float(x) * texelSize) + bitangent * (float(y) * texelSize);
            // Ограничиваем влияние смещения, чтобы сохранить направление примерно тем же
            offset *= 0.02; // Этот коэффициент можно настроить
            vec3 sampleDir = normalize(direction + offset);
            
            // Чтение значения глубины из кубической карты теней
            float closestDepth = texture(pointShadowMapsArray, vec4(sampleDir, lightIndex)).r;
            
            // Преобразуем глубину из нелинейного в линейное пространство
            closestDepth *= light.radius; // Если ваша карта теней хранит нормализованные значения
            
            // Добавляем бинарный результат сравнения глубины (в тени или нет)
            shadow += (currentDepth - bias > closestDepth) ? 1.0 : 0.0;
        }
    }
    
    // Нормализация результата и применение интенсивности
    shadow = shadow * invTotalSamples * lights.shadowIntensity;
    
    // Смягчение теней на границе радиуса действия света
    float fadeStart = light.radius * 0.85;
    if (distance > fadeStart) {
        float fadeLength = light.radius - fadeStart;
        float fadeProgress = (distance - fadeStart) / fadeLength;
        shadow *= 1.0 - fadeProgress;
    }
    
    return shadow;
}