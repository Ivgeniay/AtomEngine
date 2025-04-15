

namespace AtomEngine
{
    public interface IWorld
    {
        public void UpdateSingeThread(double deltaTime);
        public void Update(double deltaTime);
        public void Render(double deltaTime, object? context);
        public void FixedUpdate();

        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent;
        public bool HasComponent<T>(Entity entity) where T : struct, IComponent;
        public ref T AddComponent<T>(Entity entity, in T component) where T : struct, IComponent;
        public void RemoveComponent<T>(Entity entity) where T : struct, IComponent;

        public Entity CreateEntity();
        public bool IsEntityValid(uint entity_id, uint _version);
        public void DestroyEntity(Entity entity);
        public QueryEntity CreateEntityQuery();



        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1>() where T1 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2>() where T1 : struct, IComponent where T2 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3, T4>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3, T4, T5>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3, T4, T5, T6>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent;

        public IEnumerable<Entity> GetEntitiesByArchetype<T1>() where T1 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2>() where T1 : struct, IComponent where T2 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3, T4>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3, T4, T5>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent;
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3, T4, T5, T6>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent;

    }
}
