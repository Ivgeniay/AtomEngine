const int MAX_CAMERAS = 4;

struct Camera {
    vec3 position;
    float enabled;
    vec3 direction;
    float padding1;
    vec3 up;
    float padding2;
    mat4 view;
    mat4 projection;
    mat4 inverseView;
    float fov;
    float aspect;
    float near;
    float far;
    float calculateViewDirPerPixel;
    float drawDistance;
    int cameraId;
    float padding3;
};

layout(std140, binding = 3) uniform CamerasUBO {
    Camera cameras[MAX_CAMERAS];
    int activeCameraIndex;
    int numActiveCameras;
    vec2 padding;
} scene;