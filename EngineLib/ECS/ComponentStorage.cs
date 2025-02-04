

namespace EngineLib
{
    public class ComponentStorage<T> where T : struct, IComponent
    {
        private Dictionary<int, T> components = new();

        public void Add(int entityId, T component) => components[entityId] = component;
        public void Remove(int entityId) => components.Remove(entityId);
        public bool TryGet(int entityId, out T component) => components.TryGetValue(entityId, out component);
    }
}
