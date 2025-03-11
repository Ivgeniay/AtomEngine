using System.Collections.Generic;
using System;

namespace Editor
{
    internal class SystemData
    {
        public Type SystemType { get; set; }
        public string SystemName { get; set; }
        public int ExecutionOrder { get; set; } = -1;
        public List<SystemData> Dependencies { get; set; } = new List<SystemData>();
        public SystemCategory Category { get; set; }

        public SystemData Clone()
        {
            return new SystemData
            {
                SystemType = SystemType,
                SystemName = SystemName,
                ExecutionOrder = ExecutionOrder,
                Dependencies = new List<SystemData>(Dependencies),
                Category = Category
            };
        }
    }
}