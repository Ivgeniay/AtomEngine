using System.Numerics;
using AtomEngine;
using Avalonia;

namespace Editor
{
    public struct EditorCameraComponent : IComponent
    {
        public Entity Owner { get; set; }
        public Vector3 Target;
        public float MoveSpeed;
        public float RotationSpeedY;
        public float RotationSpeedX;
        public bool IsPerspective;
        public Point LastMousePosition;

        public Vector3 CurrentVelocity;
        public Vector2 CurrentRotation;
    }
}