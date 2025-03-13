using AtomEngine.RenderEntity;
using System.Numerics;

namespace AtomEngine
{
    public struct BoundingComponent : IComponent, IBoundingVolume
    {
        public Entity Owner { get; set; }

        public Vector3 Min => BoundingVolume.Min;
        public Vector3 Max => BoundingVolume.Max;

        public IBoundingVolume BoundingVolume;
        public BoundingComponent(Entity owner, IBoundingVolume boundingVolume) {
            Owner = owner;
            BoundingVolume = boundingVolume;
        }

        public BoundingComponent(Entity owner)
        {
            Owner = owner;
        }
        public Vector3[] GetVertices() => BoundingVolume.GetVertices();
        public uint[] GetIndices() => BoundingVolume.GetIndices();
        public BoundingComponent(Entity owner, MeshBase meshBase) 
        {
            Owner = owner;
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
