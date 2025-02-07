using System.Numerics;

namespace AtomEngine
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

        public Matrix4x4 GetTranslationMatrix() => Matrix4x4.CreateTranslation(Position); 
        public Matrix4x4 GetScaleMatrix() => Matrix4x4.CreateScale(Scale);
        public Matrix4x4 GetRotationMatrix()
        { 
            return Matrix4x4.CreateRotationZ(Rotation.Z) *
                   Matrix4x4.CreateRotationX(Rotation.X) *
                   Matrix4x4.CreateRotationY(Rotation.Y);
        } 

        public Matrix4x4 GetModelMatrix(Matrix4x4? parentWorldMatrix = null)
        {
            var modelMatrix = GetTranslationMatrix() * GetRotationMatrix() * GetScaleMatrix(); 
            if (parentWorldMatrix.HasValue)
            {
                return modelMatrix * parentWorldMatrix.Value;
            } 
            return modelMatrix;
        }

        public void UpdateWorldMatrix()
        {
            WorldMatrix = Matrix4x4.CreateScale(Scale) *
                         Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                         Matrix4x4.CreateTranslation(Position);
        }
    }
}
