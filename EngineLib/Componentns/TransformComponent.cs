using EngineLib;
using System.Numerics;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Transform",
    Name = "TransformComponent",
    Description = @"
    Transformation component responsible for the position, rotation and scale of an entity in 3D space.

    namespace AtomEngine

    Main properties:
    - Position - position in world space (Vector3)
    - Rotation - rotation in degrees (Vector3)
    - Scale - scale along three axes (Vector3, default 1,1,1)

    Methods:
    - GetTranslationMatrix() - returns translation matrix
    - GetRotationMatrix() - returns rotation matrix
    - GetScaleMatrix() - returns scaling matrix
    - GetModelMatrix(Matrix4x4? parentWorldMatrix = null) - returns final model matrix

    Features:
    - Uses matrix caching to optimize performance
    - Tracks changes via IsDirty flags
    - Supports hierarchical transformation via parentWorldMatrix

    Usage example:
    var entity = world.CreateEntity();
    world.AddComponent<TransformComponent>(entity);


    ",
    Author = "AtomEngine Team",
    Title = "Transformation component"
)]
    public struct TransformComponent : IComponent
    {
        public Entity Owner { get; set; }
        public Vector3 _position;
        public Vector3 _rotation;
        [DefaultVector3(1f, 1f, 1f)]
        public Vector3 _scale;
        public Vector3 Position { get => _position; set { _position = value;  IsDirtyPos = true; } }
        public Vector3 Rotation { get => _rotation; set { _rotation = value;  IsDirtyRot = true; } }
        public Vector3 Scale { get => _scale; set { _scale = value;  IsDirtyScale = true; } }

        [DefaultBool(true)]
        private bool IsDirtyPos;
        [DefaultBool(true)]
        private bool IsDirtyRot;
        [DefaultBool(true)]
        private bool IsDirtyScale;

        [HideInInspector]
        public Matrix4x4? parentWorldMatrix = null;

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
            //_translationMatrixCache = Matrix4x4.CreateTranslation(Position);
            _translationMatrixCache = AtomMath.Translate(Position);
            IsDirtyPos = false;
            return _translationMatrixCache;
        }
        public Matrix4x4 GetScaleMatrix()
        {
            //_scaleMatrixCache = Matrix4x4.CreateScale(Scale);
            _scaleMatrixCache = AtomMath.Scale(Scale);
            IsDirtyScale = false;
            return _scaleMatrixCache;
        }
        public Matrix4x4 GetRotationMatrix()
        {
            //_rotationMatrixCache = Matrix4x4.CreateRotationZ(Rotation.Z.DegreesToRadians()) *
            //                    Matrix4x4.CreateRotationX(Rotation.X.DegreesToRadians()) *
            //                    Matrix4x4.CreateRotationY(Rotation.Y.DegreesToRadians());
            //IsDirtyRot = false;
            //return _rotationMatrixCache;

            _rotationMatrixCache = AtomMath.RotateFromEuler(_rotation);
            IsDirtyRot = false;
            return _rotationMatrixCache;
        } 

        public Matrix4x4 GetModelMatrix()
        {
            if (IsDirtyPos || IsDirtyScale || IsDirtyRot)
            {
                Matrix4x4 result = Matrix4x4.Identity;
                result *= GetScaleMatrix();
                result *= GetRotationMatrix();
                result *= GetTranslationMatrix();
                _modelMatrixCache = result;
            }

            if (parentWorldMatrix.HasValue)
            {
                _modelMatrixCache *= parentWorldMatrix.Value;
            } 
            return _modelMatrixCache;
        }
    }
}