struct D_LIGHT_STRUCTURENAME {
    vec3 DIR;
    vec3 COLOR;
    float AMB_STR;
    float INTENSITY;
};

struct P_LIGHT_STRUCTURENAME {
    vec3 POS;
    vec3 COLOR;
    float AMB_STR;
    float INTENSITY;
    float CONST;
    float LINEAR;
    float QUADRATIC;
};


layout(std140) uniform LightingData {
    D_LIGHT_STRUCTURENAME DL_NAME[DL_QUAN];
    P_LIGHT_STRUCTURENAME PL_NAME[PL_QUAN];
    int DL_QUAN_NAME;
    int PL_QUAN_NAME;
};