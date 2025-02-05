using System.Numerics; 

namespace EngineLib
{
    public struct CameraComponent : IComponent
    {
        public Entity Owner { get; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }


        public Matrix4x4 ViewMatrix { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }

        public CameraComponent(Entity owner, float fieldOfView = 45.0f, float aspectRatio = 16f / 9f,
                             float nearPlane = 0.1f, float farPlane = 100.0f)
        {
            Owner = owner;
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
            NearPlane = nearPlane;
            FarPlane = farPlane;

            ViewMatrix = Matrix4x4.Identity;
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView * (MathF.PI / 180f), // конвертируем градусы в радианы
                AspectRatio,
                NearPlane,
                FarPlane
            );
        }
    }
}
