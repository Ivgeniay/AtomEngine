const int MAX_CAMERAS = 4;

struct Camera {
    vec3 position;
    vec3 front;
    vec3 up;
    float fov;
    float aspectRatio;
    float nearPlane;
    float farPlane;
    float enabled;
    mat4 viewMatrix;
    mat4 projectionMatrix;
};

layout(std140, binding = 3) uniform CamerasUBO {
    Camera cameras[MAX_CAMERAS];
    int activeCameraIndex;
} cameraData;
