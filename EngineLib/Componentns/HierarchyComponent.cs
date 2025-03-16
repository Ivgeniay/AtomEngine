using EngineLib;

namespace AtomEngine
{
    [HideClose]
    public struct HierarchyComponent : IComponent
    {
        public Entity Owner { get ; set ;}
        [ReadOnly]
        public uint Level;
        [ReadOnly]
        public uint Parent;
        [ReadOnly]
        public List<uint> Children;
    }
}
