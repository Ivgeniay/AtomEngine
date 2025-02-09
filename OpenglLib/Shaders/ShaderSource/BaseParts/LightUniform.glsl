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


layout(std140) uniform LightingData {
    DirectionalLight DL_NAME[DL_QUAN];
    PointLight PL_NAME[PL_QUAN];
    int DL_QUAN_NAME;
    int PL_QUAN_NAME;
};