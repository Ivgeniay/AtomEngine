namespace AtomEngine
{
    public struct Entity
    {
        public readonly uint Id { get;}
        public readonly uint Version { get;}

        internal Entity(uint id, uint version)
        {
            Version = version;
            Id = id;
        }
    }
}
