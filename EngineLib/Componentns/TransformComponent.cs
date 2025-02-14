using System.Numerics;

namespace AtomEngine
{
    public struct TransformComponent : IComponent
    {
        public Entity Owner { get; }
        public Vector3 _position;
        public Vector3 _rotation;
        public Vector3 _scale;
        public Vector3 Position { get => _position; set { _position = value; _world.BvhPool.MarkNodeDirty(Owner); IsDirty = true; } }
        public Vector3 Rotation { get => _rotation; set { _rotation = value; _world.BvhPool.MarkNodeDirty(Owner); IsDirty = true; } }
        public Vector3 Scale { get => _scale; set { _scale = value; _world.BvhPool.MarkNodeDirty(Owner); IsDirty = true; } }
        
        public Matrix4x4 WorldMatrix;

        private bool IsDirty = false;
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
            WorldMatrix = Matrix4x4.Identity;
        }

        public Matrix4x4 GetTranslationMatrix()
        {
            if (IsDirty)
            {
                _translationMatrix = Matrix4x4.CreateTranslation(Position);
            }
            return _translationMatrix;
        }
        public Matrix4x4 GetScaleMatrix()
        {
            if (IsDirty)
            {
                _scaleMatrix = Matrix4x4.CreateScale(Scale);
            }
            return _scaleMatrix;
        }
        public Matrix4x4 GetRotationMatrix()
        {
            if (IsDirty)
            {
                _rotationMatrix = Matrix4x4.CreateRotationZ(Rotation.Z) *
                                  Matrix4x4.CreateRotationX(Rotation.X) *
                                  Matrix4x4.CreateRotationY(Rotation.Y);
            }
            return _rotationMatrix;
        } 

        public Matrix4x4 GetModelMatrix(Matrix4x4? parentWorldMatrix = null)
        {
            if (IsDirty)
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
