vec3 calculateDirectionalLight(DirectionalLight light, vec3 baseColor, vec3 normal) {
    vec3 normalizedNormal = normalize(normal);
    vec3 normalizedLightDir = normalize(-light.DIR);

    vec3 ambient = light.AMB_STR * baseColor;
    float diff = max(dot(normalizedNormal, normalizedLightDir), 0.0);
    vec3 diffuse = diff * baseColor * light.COLOR * light.INTENSITY;

    return ambient + diffuse;
}

vec3 calculatePointLight(PointLight light, vec3 baseColor, vec3 normal, vec3 fragPos) {
    vec3 normalizedNormal = normalize(normal);
    vec3 lightDir = light.POS - fragPos;
    float distance = length(lightDir);
    lightDir = normalize(lightDir);

    float attenuation = 1.0 / (light.CONST + light.LINEAR * distance + light.QUADRATIC * distance * distance);
    attenuation = max(attenuation, 0.0001);

    vec3 ambient = light.AMB_STR * baseColor;
    float diff = max(dot(normalizedNormal, lightDir), 0.0);
    vec3 diffuse = diff * baseColor * light.COLOR * light.INTENSITY;

    return (ambient + diffuse) * attenuation;
}

vec3 calculateLighting(vec3 baseColor, vec3 normal, vec3 fragPos) {
    vec3 result = vec3(0.0);

    for (int i = 0; i < DL_QUAN; i++) {
        if (length(DL_NAME[i].COLOR) < 0.0001 || DL_NAME[i].INTENSITY < 0.0001) {
            continue;
        }
        float isActive = float(i < DL_QUAN_NAME);
        result += calculateDirectionalLight(DL_NAME[i], baseColor, normal) * isActive;
    }

    for (int i = 0; i < PL_QUAN; i++) {
        if (length(PL_NAME[i].COLOR) < 0.0001 || PL_NAME[i].INTENSITY < 0.0001) {
            continue;
        }
        float isActive = float(i < PL_QUAN_NAME);
        result += calculatePointLight(PL_NAME[i], baseColor, normal, fragPos) * isActive;
    }

    if (length(result) < 0.0001) return baseColor;
    return result;
}



