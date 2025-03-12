using System.Collections.Generic;
using System;

namespace Editor
{
    internal class SystemData : ICloneable
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
                Dependencies = new List<SystemData>(Dependencies),
                IncludInWorld = new List<uint>(IncludInWorld),
                Category = Category
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}