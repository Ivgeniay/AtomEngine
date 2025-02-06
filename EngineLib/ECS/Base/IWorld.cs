

namespace EngineLib
{
    public interface IWorld
    {
        public void Update(float deltaTime);
        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent;
        public bool HasComponent<T>(Entity entity) where T : struct, IComponent;
        public ref T AddComponent<T>(Entity entity, in T component) where T : struct, IComponent;
        public void RemoveComponent<T>(Entity entity) where T : struct, IComponent;

        public Entity CreateEntity();
        public void DestroyEntity(Entity entity);
        public Query CreateQuery();

        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1>() where T1 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2>() where T1 : struct, IComponent where T2 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent;

        public IEnumerable<Entity> GetEntitiesByArchetype<T1>() where T1 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2>() where T1 : struct, IComponent where T2 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent;
    }
}
