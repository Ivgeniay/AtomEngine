namespace AtomEngine
{
    public struct Entity
    {
        public readonly uint Id { get;}
        public readonly uint Version { get;}

        public Entity(uint id, uint version)
        {
            Version = version;
            Id = id;
        }

        public static Entity Null => new Entity(uint.MaxValue, uint.MaxValue);
        public static bool operator ==(Entity a, Entity b) => a.Id == b.Id && a.Version == b.Version;
        public static bool operator !=(Entity a, Entity b) => a.Id != b.Id || a.Version != b.Version;

        public override string ToString() => $"Entity: Id({Id}) Version({Version})";
        public override bool Equals(object obj) => obj is Entity entity && this == entity;
        public override int GetHashCode() => HashCode.Combine(Id, Version); 
    }
}
