namespace AtomEngine
{
    public struct PhysicsMaterialComponent : IComponent
    {
        private Entity _owner;
        public Entity Owner => _owner;
        public PhysicsMaterialComponent(Entity owner, PhysicsMaterial material)
        {
            _owner = owner;
            Material = material;
        }

        public PhysicsMaterialComponent(Entity owner)
        {
            _owner = owner;
            Material = PhysicsMaterial.Default;
        }

        public PhysicsMaterial Material { get; set; }
    }
}
