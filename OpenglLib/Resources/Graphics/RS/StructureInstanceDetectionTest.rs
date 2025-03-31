[InterfaceName:IMaterialRenderer]
[ComponentName:MaterialComponent]
[SystemName:MaterialRenderSystem]
[PlaceTarget:both]

struct SurfaceMaterial {
    float shininess;
    float specularStrength;
    vec3 baseColor;
};

// uniform инстанс структуры
[PlaceTarget:both]
uniform SurfaceMaterial surfaceMat;

// Определение другой структуры с инстансами
[PlaceTarget:both]
struct LightInfo {
    vec3 position;
    vec3 color;
    float intensity;
} mainLight, secondaryLight;

// Функция для расчета освещения
[PlaceTarget:both]
vec3 calculateLighting(SurfaceMaterial material, LightInfo light, vec3 normal, vec3 viewDir) {
    // Внутри функции определение не должно учитываться
    SurfaceMaterial localMaterial;
    
    vec3 lightDir = normalize(light.position);
    float diff = max(dot(normal, lightDir), 0.0);
    
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    vec3 diffuse = diff * material.baseColor * light.color;
    vec3 specular = spec * material.specularStrength * light.color;
    
    return (diffuse + specular) * light.intensity;
}

// Массив структур
[PlaceTarget:both]
LightInfo additionalLights[4];

// Uniform блок со структурой внутри (не должен учитываться)
[PlaceTarget:both]
layout(std140) uniform MaterialBlock {
    SurfaceMaterial materials[2];
};