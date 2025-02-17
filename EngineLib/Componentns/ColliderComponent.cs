using System.Numerics;

namespace AtomEngine
{
    public struct ColliderComponent : IComponent
    {
        public Entity Owner { get; }
        private ICollider _collider;

        // Делаем поля публичными с возможностью чтения и записи
        public Vector3 LocalOffset { get; set; }
        public Quaternion LocalRotation { get; set; }

        public ColliderComponent(Entity owner, ICollider collider,
            Vector3 localOffset = default, Quaternion localRotation = default)
        {
            Owner = owner;
            _collider = collider;
            LocalOffset = localOffset;
            LocalRotation = localRotation == default ? Quaternion.Identity : localRotation;
        }

        public Vector3 GetSupport(Vector3 direction)
        {
            Vector3 localDir = Vector3.Transform(direction, Quaternion.Inverse(LocalRotation));
            Vector3 localSupport = _collider.GetSupport(localDir);
            return Vector3.Transform(localSupport, LocalRotation) + LocalOffset;
        }

        public IBoundingVolume ComputeBounds()
        {
            var localBounds = _collider.ComputeBounds();
            return localBounds.Transform(
                Matrix4x4.CreateFromQuaternion(LocalRotation) *
                Matrix4x4.CreateTranslation(LocalOffset));
        }
    }
}
