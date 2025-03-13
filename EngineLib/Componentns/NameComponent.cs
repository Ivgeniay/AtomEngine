namespace AtomEngine
{
    public struct NameComponent : IComponent
    {
        public string Name { get; set; }
        public Entity Owner {  get; set; }

        public NameComponent(Entity owner, string name)
        {
            Name = name;
            Owner = owner;
        }
    }
}
