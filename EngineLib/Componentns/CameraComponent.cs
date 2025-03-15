using EngineLib;
using System.Numerics; 

namespace AtomEngine
{
    public struct CameraComponent : IComponent
    {
        public Entity Owner { get; set; }
        [Min(0.1)]
        public float FieldOfView;
        [Min(0.1)]
        public float AspectRatio;
        [Min(0.001)]
        public float NearPlane;
        [Min(0.01)]
        public float FarPlane;

        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
        public Vector3 CameraUp = Vector3.UnitY; 
        public Vector3 CameraFront = new Vector3(0.0f, 0.0f, 1.0f);

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
