namespace AtomEngine
{
    public struct PhysicsMaterial
    {
        public float Restitution { get; set; }    
        public float StaticFriction { get; set; } 
        public float DynamicFriction { get; set; }

        public static PhysicsMaterial Create(float restitution, float staticFriction, float dynamicFriction)
        {
            return new PhysicsMaterial
            {
                Restitution = restitution,
                StaticFriction = staticFriction,
                DynamicFriction = dynamicFriction
            };
        }

        public static PhysicsMaterial Default => Create(0.5f, 0.5f, 0.3f);
        public static PhysicsMaterial Ice => Create(0.1f, 0.02f, 0.01f);
        public static PhysicsMaterial Rubber => Create(0.8f, 0.9f, 0.7f);
        public static PhysicsMaterial Wood => Create(0.3f, 0.4f, 0.3f);
        public static PhysicsMaterial Metal => Create(0.6f, 0.4f, 0.2f);
    }
}
