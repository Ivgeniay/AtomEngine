using System.Numerics;

namespace AtomEngine
{
    public partial struct GlobalLightSettingsComponent : IComponent
    {
        public Entity Owner { get; set; }

        [DefaultVector3(0.1f, 0.1f, 0.1f)]
        public Vector3 AmbientColor;
        [DefaultFloat(0.1f)]
        public float AmbientIntensity;
        [DefaultFloat(0.0085f)]
        public float ShadowBias;
        [DefaultInt(3)]
        public int PcfKernelSize;
        [DefaultFloat(0.7f)]
        public float ShadowIntensity;

        [DefaultBool(true)]
        public bool IsDirty = true;

        public GlobalLightSettingsComponent(Entity entity)
        {
            Owner = entity;
            AmbientColor = new Vector3(1, 1, 1);
            AmbientIntensity = 1;
            ShadowBias = 0.0085f;
            PcfKernelSize = 3;
            ShadowIntensity = 0.7f;
            IsDirty = true;
        }

        public void MakeClean()
        {
            IsDirty = false;
        }
    }
}
