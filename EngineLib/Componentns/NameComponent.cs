namespace AtomEngine
{
    public struct NameComponent : IComponent
    {
        public string Name { get; set; }
        private Entity _entity { get; set; }
        public Entity Owner => _entity;

        public NameComponent(Entity owner, string name)
        {
            Name = name;
            _entity = owner;
        }
    }
}
