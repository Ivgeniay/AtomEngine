using EngineLib;
using System.Numerics; 

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Rendering",
    Name = "CameraComponent",
    Description = @"
    Camera component for viewing and displaying the scene.

    namespace AtomEngine

    Main properties:
    - FieldOfView - field of view in degrees (min. 0.1)
    - AspectRatio - aspect ratio (width/height) (min. 0.1)
    - NearPlane - near clipping plane (min. 0.001)
    - FarPlane - far clipping plane (min. 0.01)
    - ViewMatrix - current view matrix
    - CameraUp - camera up vector (default (0,1,0))
    - CameraFront - camera front vector (default (0,0,1))

    Methods:
    - CreateProjectionMatrix() - creates a projection matrix for the camera

    Usage example:
    var cameraEntity = world.CreateEntity();
    var camera = world.AddComponent<CameraComponent>(cameraEntity);
    var transform = cameraEntity.GetComponent<TransformComponent>();
    transform.Position = new Vector3(0, 5, -10);

    // Getting matrices for rendering
    var viewMatrix = camera.ViewMatrix;
    var projectionMatrix = camera.CreateProjectionMatrix();
    ",
    Author = "AtomEngine Team",
    Title = "Camera Component"
)]
    [TooltipCategoryComponent(ComponentCategory.Render)]
    public struct CameraComponent : IComponent
    {
        public Entity Owner { get; set; }
        [Min(0.1)]
        [DefaultFloat(45f)]
        public float FieldOfView;
        [Min(0.1)]
        [DefaultFloat(16f/9f)]
        public float AspectRatio;
        [Min(0.001)]
        [DefaultFloat(0.1f)]
        public float NearPlane;
        [Min(0.01)]
        [DefaultFloat(200f)]
        public float FarPlane;

        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
        [DefaultVector3(0f, 1f, 0f)]
        public Vector3 CameraUp; 
        [DefaultVector3(0f, 0f, 1f)]
        public Vector3 CameraFront;

        public CameraComponent(Entity owner, float fieldOfView = 45.0f, float aspectRatio = 16f / 9f,
                             float nearPlane = 0.1f, float farPlane = 200.0f)
        {
            Owner = owner;
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
            NearPlane = nearPlane;
            FarPlane = farPlane; 
        }
        

        public Matrix4x4 CreateProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView * (MathF.PI / 180f),
                AspectRatio,
                NearPlane,
                FarPlane
            );
        }
    }
}
