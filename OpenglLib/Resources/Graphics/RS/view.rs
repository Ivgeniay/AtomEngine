uniform vec3 modelPosition;
uniform vec3 modelRotation;
uniform vec3 modelScale;

mat4 createModelMatrix() {
    vec3 rotRad = radians(modelRotation);
    
    mat4 rotX = mat4(
        1.0, 0.0, 0.0, 0.0,
        0.0, cos(rotRad.x), -sin(rotRad.x), 0.0,
        0.0, sin(rotRad.x), cos(rotRad.x), 0.0,
        0.0, 0.0, 0.0, 1.0
    );
    
    mat4 rotY = mat4(
        cos(rotRad.y), 0.0, sin(rotRad.y), 0.0,
        0.0, 1.0, 0.0, 0.0,
        -sin(rotRad.y), 0.0, cos(rotRad.y), 0.0,
        0.0, 0.0, 0.0, 1.0
    );
    
    mat4 rotZ = mat4(
        cos(rotRad.z), -sin(rotRad.z), 0.0, 0.0,
        sin(rotRad.z), cos(rotRad.z), 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0
    );
    
    mat4 scale = mat4(
        modelScale.x, 0.0, 0.0, 0.0,
        0.0, modelScale.y, 0.0, 0.0,
        0.0, 0.0, modelScale.z, 0.0,
        0.0, 0.0, 0.0, 1.0
    );
    
    mat4 translate = mat4(
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        modelPosition.x, modelPosition.y, modelPosition.z, 1.0
    );
    
    return translate * rotZ * rotY * rotX * scale;
}