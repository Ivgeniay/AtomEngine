namespace AtomEngine
{
    public struct StaticComponent : IComponent
    {
        public Entity Owner { get; set; }
        public StaticComponent(Entity owner) => Owner = owner;
    }
}
