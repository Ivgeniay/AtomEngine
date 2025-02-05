using System.Numerics;

namespace EngineLib
{
    public struct TransformComponent : IComponent
    {
        public Entity Owner { get; }
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Matrix4x4 WorldMatrix;

        public TransformComponent(Entity owner)
        {
            Owner = owner;
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            WorldMatrix = Matrix4x4.Identity;
        }

        public void UpdateWorldMatrix()
        {
            WorldMatrix = Matrix4x4.CreateScale(Scale) *
                         Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                         Matrix4x4.CreateTranslation(Position);
        }
    }
}
