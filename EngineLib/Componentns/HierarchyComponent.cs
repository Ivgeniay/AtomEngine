using EngineLib;

namespace AtomEngine
{
    [HideClose]
    public struct HierarchyComponent : IComponent
    {
        public Entity Owner { get ; set ;}
        public uint Parent;
        public List<uint> Children;
    }
}
