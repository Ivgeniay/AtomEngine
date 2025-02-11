struct DirectionalLight {
    vec3 DIR;
    vec3 COLOR;
    float AMB_STR;
    float INTENSITY;
};

struct PointLight {
    vec3 POS;
    vec3 COLOR;
    float AMB_STR;
    float INTENSITY;
    float CONST;
    float LINEAR;
    float QUADRATIC;
}; 

struct MaterialData {
    vec3 diffuseColor;
    vec3 specularColor;
    float shininess;
};

struct LightData {
    vec3 position;
    vec3 direction;
    vec3 color;
    float intensity;
};

struct SceneData {
    MaterialData materials[3];
    LightData lights[5];
    vec3 directionData[3];
    vec3 ambientColor;
};




layout(std140) uniform LightingData {
    DirectionalLight DL_NAME[DL_QUAN];
    PointLight PL_NAME[PL_QUAN];
    int DL_QUAN_NAME;
    int PL_QUAN_NAME;
};