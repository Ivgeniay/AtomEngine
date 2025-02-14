using AtomEngine.RenderEntity;
using System.Numerics;

namespace AtomEngine
{
    public struct BoundingComponent : IComponent, IBoundingVolume
    {
        private Entity _owner;
        public Entity Owner => _owner;

        public Vector3 Min => BoundingVolume.Min;
        public Vector3 Max => BoundingVolume.Max;

        public IBoundingVolume BoundingVolume;
        public BoundingComponent(Entity owner, IBoundingVolume boundingVolume) {
            _owner = owner;
            BoundingVolume = boundingVolume;
        }

        public BoundingComponent(Entity owner)
        {
            _owner = owner;
        }

        public BoundingComponent(Entity owner, MeshBase meshBase) 
        {
            _owner = owner;
            BoundingVolume = BoundingBox.ComputeBoundingBox(meshBase.Vertices_);
        }

        public BoundingComponent FromSphere(MeshBase meshBase)
        {
            BoundingVolume = BoundingSphere.ComputeBoundingSphereRitter(meshBase.Vertices_);
            return this;
        }

        public BoundingComponent FromBox(MeshBase meshBase)
        {
            BoundingVolume = BoundingBox.ComputeBoundingBox(meshBase.Vertices_);
            return this;
        }

        public bool Intersects(IBoundingVolume other) => BoundingVolume.Intersects(other);
        public IBoundingVolume Transform(Matrix4x4 modelMatrix) => BoundingVolume.Transform(modelMatrix);
    }
}
