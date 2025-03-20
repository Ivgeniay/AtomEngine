namespace AtomEngine
{
    public class SystemData : ICloneable
    {
        public string SystemFullTypeName { get; set; } = string.Empty;
        public int ExecutionOrder { get; set; } = -1;
        public List<SystemData> Dependencies { get; set; } = new List<SystemData>();
        public List<uint> IncludInWorld { get; set; } = new List<uint>();
        public SystemCategory Category { get; set; }

        public SystemData Clone()
        {
            return new SystemData
            {
                SystemFullTypeName = SystemFullTypeName,
                ExecutionOrder = ExecutionOrder,
                Dependencies = this.Dependencies,
                IncludInWorld = new List<uint>(IncludInWorld),
                Category = Category
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public override bool Equals(object obj)
        {
            if (obj is not SystemData other) return false;
            return SystemFullTypeName == other.SystemFullTypeName;
        }

        public override int GetHashCode()
        {
            return SystemFullTypeName?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return SystemFullTypeName;
        }
    }
}
