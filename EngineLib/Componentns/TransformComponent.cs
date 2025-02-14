using System.Numerics;

namespace AtomEngine
{
    public struct TransformComponent : IComponent
    {
        public Entity Owner { get; }
        public Vector3 _position;
        public Vector3 _rotation;
        public Vector3 _scale;
        public Vector3 Position { get => _position; set { _position = value; _world.BvhPool.MarkNodeDirty(Owner); IsDirtyPos = true; } }
        public Vector3 Rotation { get => _rotation; set { _rotation = value; _world.BvhPool.MarkNodeDirty(Owner); IsDirtyRot = true; } }
        public Vector3 Scale { get => _scale; set { _scale = value; _world.BvhPool.MarkNodeDirty(Owner); IsDirtyScale = true; } }
        

        private bool IsDirtyPos = false;
        private bool IsDirtyRot = false;
        private bool IsDirtyScale = false;
        private World _world;

        private Matrix4x4 _translationMatrix;
        private Matrix4x4 _rotationMatrix;
        private Matrix4x4 _scaleMatrix;
        private Matrix4x4 _modelMatrix;

        public TransformComponent(Entity owner, World world)
        {
            _world = world;
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
                _rotationMatrix = Matrix4x4.CreateRotationZ(Rotation.Z) *
                                  Matrix4x4.CreateRotationX(Rotation.X) *
                                  Matrix4x4.CreateRotationY(Rotation.Y);
                IsDirtyRot = false;
            }
            return _rotationMatrix;
        } 

        public Matrix4x4 GetModelMatrix(Matrix4x4? parentWorldMatrix = null)
        {
            if (IsDirtyPos || IsDirtyScale || IsDirtyRot)
            {
                _modelMatrix = GetTranslationMatrix() * GetRotationMatrix() * GetScaleMatrix();
            }
            if (parentWorldMatrix.HasValue)
            {
                _modelMatrix *= parentWorldMatrix.Value;
            } 
            return _modelMatrix;
        }
    }
}
