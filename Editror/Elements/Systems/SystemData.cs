using System.Collections.Generic;

namespace Editor
{
    internal class SystemData
    {
        public string SystemFullTypeName { get; set; } = string.Empty;
        public int ExecutionOrder { get; set; } = -1;
        public List<SystemData> Dependencies { get; set; } = new List<SystemData>();
        public SystemCategory Category { get; set; }

        public SystemData Clone()
        {
            return new SystemData
            {
                SystemFullTypeName = SystemFullTypeName,
                ExecutionOrder = ExecutionOrder,
                Dependencies = new List<SystemData>(Dependencies),
                Category = Category
            };
        }
    }
}