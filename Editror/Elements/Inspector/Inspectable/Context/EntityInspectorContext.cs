using AtomEngine;

namespace Editor
{
    public class EntityInspectorContext : InspectorContext
    {
        public uint EntityId { get; set; }
        public IComponent Component { get; set; }
    }
}
