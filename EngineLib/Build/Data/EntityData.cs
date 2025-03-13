namespace AtomEngine
{
    public class EntityData
    {
        public uint Id { get; set; }
        public uint Version { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, IComponent> Components { get; set; } = new();

        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public bool IsPrefabInstance { get; set; } = false;
        public string PrefabSourceGuid { get; set; } = string.Empty;

    }
}
