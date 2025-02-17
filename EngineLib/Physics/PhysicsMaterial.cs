using BepuPhysics.Constraints;

namespace AtomEngine
{
    public struct PhysicsMaterial
    {
        public float Bounciness { get; set; }
        public float StaticFriction { get; set; }
        public float DynamicFriction { get; set; }

        public PhysicMaterialCombine FrictionCombine { get; set; }
        public PhysicMaterialCombine BounceCombine { get; set; }

        public float AngularFrequency { get; set; }
        public float DampingRatio { get; set; }

        public static PhysicsMaterial Create(
            float bounciness,
            float staticFriction,
            float dynamicFriction,
            PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Average,
            PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Average,
            float frequency = 30f,
            float dampingRatio = 1f)
        {
            return new PhysicsMaterial
            {
                Bounciness = bounciness,
                StaticFriction = staticFriction,
                DynamicFriction = dynamicFriction,
                FrictionCombine = frictionCombine,
                BounceCombine = bounceCombine,
                AngularFrequency = frequency,
                DampingRatio = dampingRatio
            };
        }

        public static PhysicsMaterial Default => Create(0.5f, 0.6f, 0.6f, PhysicMaterialCombine.Average);
        public static PhysicsMaterial Rubber => Create(0.8f, 1.0f, 0.8f, PhysicMaterialCombine.Maximum);
        public static PhysicsMaterial Wood => Create(0.5f, 0.45f, 0.45f, PhysicMaterialCombine.Average);
        public static PhysicsMaterial Metal => Create(0.3f, 0.6f, 0.4f, PhysicMaterialCombine.Average);
        public static PhysicsMaterial Ice => Create(0.1f,0.02f, 0.01f, PhysicMaterialCombine.Minimum);
    }
}
