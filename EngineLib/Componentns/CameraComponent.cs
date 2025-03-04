﻿using System.Numerics; 

namespace AtomEngine
{
    public struct CameraComponent : IComponent
    {
        public Entity Owner { get; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }

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
