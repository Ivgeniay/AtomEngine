using EngineLib;
using System.Numerics;
using System.Runtime.InteropServices;

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
        [DefaultBool(true)]
        [IgnoreChangingScene]
        public bool IsActive;

        public CameraComponent(Entity owner)
        {
            Owner = owner;
            FieldOfView = 45;
            AspectRatio = 1.777f;
            NearPlane = 0.1f;
            FarPlane = 45;
            CameraUp = new Vector3(0, 1, 0);
            CameraFront = new Vector3(0, 0, 1);
        }
        public CameraComponent(Entity owner, float fieldOfView = 45.0f, float aspectRatio = 16f / 9f,
                             float nearPlane = 0.1f, float farPlane = 200.0f)
        {
            Owner = owner;
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
            NearPlane = nearPlane;
            FarPlane = farPlane;
            CameraUp = new Vector3(0, 1, 0);
            CameraFront = new Vector3(0, 0, 1);
        }
        /*
        FieldOfView = 45,
        AspectRatio = 1.777f,
        NearPlane = 0.1f,
        FarPlane = 45,
        CameraUp = new Vector3(0, 1, 0),
        CameraFront = new Vector3(0, 0, 1)
         */

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

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    public struct CameraData
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(16)]
        public Vector3 Front;

        [FieldOffset(32)]
        public Vector3 Up;

        [FieldOffset(44)]
        public float Fov;

        [FieldOffset(48)]
        public float AspectRatio;

        [FieldOffset(52)]
        public float NearPlane;

        [FieldOffset(56)]
        public float FarPlane;

        [FieldOffset(60)]
        public float Enabled;

        [FieldOffset(64)]
        public Matrix4x4 ViewMatrix;

        [FieldOffset(128)]
        public Matrix4x4 ProjectionMatrix;
    }

    [StructLayout(LayoutKind.Explicit, Size = 784)]
    public struct CamerasUboData
    {
        [FieldOffset(0)]
        public CameraData Camera0;

        [FieldOffset(192)]
        public CameraData Camera1;

        [FieldOffset(384)]
        public CameraData Camera2;

        [FieldOffset(576)]
        public CameraData Camera3;

        [FieldOffset(768)]
        public int ActiveCameraIndex;
    }
}
