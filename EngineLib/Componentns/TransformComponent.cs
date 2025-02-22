using System.Numerics;

namespace AtomEngine
{
    public struct TransformComponent : IComponent
    {
        public Entity Owner { get; }
        public string Namess = "kek";

        public Vector3 _position;
        public Vector3 _rotation;
        public Vector3 _scale;
        public Vector3 Position { get => _position; set { _position = value;  IsDirtyPos = true; } }
        public Vector3 Rotation { get => _rotation; set { _rotation = value;  IsDirtyRot = true; } }
        public Vector3 Scale { get => _scale; set { _scale = value;  IsDirtyScale = true; } }
        

        private bool IsDirtyPos = false;
        private bool IsDirtyRot = false;
        private bool IsDirtyScale = false;

        private Matrix4x4 _translationMatrix;
        private Matrix4x4 _rotationMatrix;
        private Matrix4x4 _scaleMatrix;
        private Matrix4x4 _modelMatrix;

        public TransformComponent(Entity owner)
        {
            Owner = owner;
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
        }

        public Matrix4x4 GetTranslationMatrix()
        {
            if (IsDirtyPos)
            {
                _translationMatrix = Matrix4x4.CreateTranslation(Position);
                IsDirtyPos = false;
            }
            return _translationMatrix;
        }
        public Matrix4x4 GetScaleMatrix()
        {
            if (IsDirtyScale)
            {
                _scaleMatrix = Matrix4x4.CreateScale(Scale);
                IsDirtyScale = false;
            }
            return _scaleMatrix;
        }
        public Matrix4x4 GetRotationMatrix()
        {
            if (IsDirtyRot)
            {
                _rotationMatrix = Matrix4x4.CreateRotationZ(Rotation.Z.DegreesToRadians()) *
                                  Matrix4x4.CreateRotationX(Rotation.X.DegreesToRadians()) *
                                  Matrix4x4.CreateRotationY(Rotation.Y.DegreesToRadians());
                IsDirtyRot = false;
            }
            return _rotationMatrix;
        } 

        public Matrix4x4 GetModelMatrix(Matrix4x4? parentWorldMatrix = null)
        {
            if (IsDirtyPos || IsDirtyScale || IsDirtyRot)
            {
                _modelMatrix = GetScaleMatrix() * GetRotationMatrix() * GetTranslationMatrix();
            }
            if (parentWorldMatrix.HasValue)
            {
                _modelMatrix *= parentWorldMatrix.Value;
            } 
            return _modelMatrix;
        }
    }
}
