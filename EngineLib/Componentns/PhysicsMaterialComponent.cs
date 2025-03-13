namespace AtomEngine
{
    public struct PhysicsMaterialComponent : IComponent
    {
        public Entity Owner { get; set; }
        public PhysicsMaterialComponent(Entity owner, PhysicsMaterial material)
        {
            Owner = owner;
            Material = material;
        }

        public PhysicsMaterialComponent(Entity owner)
        {
            Owner = owner;
            Material = PhysicsMaterial.Default;
        }

        public PhysicsMaterial Material { get; set; }
    }
}
