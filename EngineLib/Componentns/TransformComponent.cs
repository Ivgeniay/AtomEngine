using EngineLib;
using System.Numerics;

namespace AtomEngine
{
    public struct TransformComponent : IComponent
    {
        public Entity Owner { get; set; }
        public Vector3 _position;
        public Vector3 _rotation;
        public Vector3 _scale;
        public Vector3 Position { get => _position; set { _position = value;  IsDirtyPos = true; } }
        public Vector3 Rotation { get => _rotation; set { _rotation = value;  IsDirtyRot = true; } }
        public Vector3 Scale { get => _scale; set { _scale = value;  IsDirtyScale = true; } }

        private bool IsDirtyPos = false;
        private bool IsDirtyRot = false;
        private bool IsDirtyScale = false;

        private Matrix4x4 _translationMatrixCache;
        private Matrix4x4 _rotationMatrixCache;
        private Matrix4x4 _scaleMatrixCache;
        private Matrix4x4 _modelMatrixCache;

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
                _translationMatrixCache = Matrix4x4.CreateTranslation(Position);
                IsDirtyPos = false;
            }
            return _translationMatrixCache;
        }
        public Matrix4x4 GetScaleMatrix()
        {
            if (IsDirtyScale)
            {
                _scaleMatrixCache = Matrix4x4.CreateScale(Scale);
                IsDirtyScale = false;
            }
            return _scaleMatrixCache;
        }
        public Matrix4x4 GetRotationMatrix()
        {
            if (IsDirtyRot)
            {
                _rotationMatrixCache = Matrix4x4.CreateRotationZ(Rotation.Z.DegreesToRadians()) *
                                  Matrix4x4.CreateRotationX(Rotation.X.DegreesToRadians()) *
                                  Matrix4x4.CreateRotationY(Rotation.Y.DegreesToRadians());
                IsDirtyRot = false;
            }
            return _rotationMatrixCache;
        } 

        public Matrix4x4 GetModelMatrix(Matrix4x4? parentWorldMatrix = null)
        {
            if (IsDirtyPos || IsDirtyScale || IsDirtyRot)
            {
                _modelMatrixCache = GetScaleMatrix() * GetRotationMatrix() * GetTranslationMatrix();
            }
            if (parentWorldMatrix.HasValue)
            {
                _modelMatrixCache *= parentWorldMatrix.Value;
            } 
            return _modelMatrixCache;
        }
    }
}
