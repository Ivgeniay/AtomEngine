namespace AtomEngine
{
    public struct StaticComponent : IComponent
    {
        private Entity _owner;
        public Entity Owner => _owner;
        public StaticComponent(Entity owner) => _owner = owner;
    }
}
