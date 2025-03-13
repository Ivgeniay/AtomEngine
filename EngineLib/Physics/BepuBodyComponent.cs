using BepuPhysics;

namespace AtomEngine
{
    public struct BepuBodyComponent : IComponent
    {
        public Entity Owner { get; set; }
        public BodyHandle? DynamicHandle; 
        public StaticHandle? StaticHandle;
        public BodyType BodyType;

        public BepuBodyComponent(Entity owner, BodyHandle handle, BodyType bodyType)
        {
            if (bodyType == BodyType.Static)
                throw new ArgumentException("BodyType must be Dynamic or Kinematic for dynamic body", nameof(bodyType));
            Owner = owner;
            DynamicHandle = handle;
            StaticHandle = null;
            BodyType = bodyType;
        }

        public BepuBodyComponent(Entity owner, StaticHandle handle)
        {
            Owner = owner;
            DynamicHandle = null;
            StaticHandle = handle;
            BodyType = BodyType.Static;
        }
    }
}
