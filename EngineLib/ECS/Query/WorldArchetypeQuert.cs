
namespace EngineLib
{
    public partial class World
    {
        // Вспомогательные методы для получения архетипов
        public IEnumerable<Entity> GetEntitiesByArchetype<T>() where T : struct, IComponent
        {
            return CreateEntitiesFromSpan(_archetypePool.GetEntitiesWith<T>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            return CreateEntitiesFromSpan(_archetypePool.GetEntitiesWith<T1, T2>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            return CreateEntitiesFromSpan(_archetypePool.GetEntitiesWith<T1, T2, T3>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            return CreateEntitiesFromSpan(_archetypePool.GetEntitiesWith<T1, T2, T3, T4>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            return CreateEntitiesFromSpan(_archetypePool.GetEntitiesWith<T1, T2, T3, T4, T5>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            return CreateEntitiesFromSpan(_archetypePool.GetEntitiesWith<T1, T2, T3, T4, T5, T6>());
        }

        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T>() where T : struct, IComponent
        {
            return CreateEntitiesFromIds(_archetypePool.GetEntitiesHaving<T>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            return CreateEntitiesFromIds(_archetypePool.GetEntitiesHaving<T1, T2>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            return CreateEntitiesFromIds(_archetypePool.GetEntitiesHaving<T1, T2, T3>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            return CreateEntitiesFromIds(_archetypePool.GetEntitiesHaving<T1, T2, T3, T4>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            return CreateEntitiesFromIds(_archetypePool.GetEntitiesHaving<T1, T2, T3, T4, T5>());
        }
        public IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            return CreateEntitiesFromIds(_archetypePool.GetEntitiesHaving<T1, T2, T3, T4, T5, T6>());
        }

        private IEnumerable<Entity> CreateEntitiesFromIds(IEnumerable<uint> entityIds)
        {
            foreach (var id in entityIds)
            {
                if (_entityVersions.TryGetValue(id, out uint version))
                {
                    yield return new Entity(id, version);
                }
            }
        }

        private Entity[] CreateEntitiesFromSpan(ReadOnlySpan<uint> entityIds)
        {
            var entities = new List<Entity>(entityIds.Length);
            foreach (var id in entityIds)
            {
                if (_entityVersions.TryGetValue(id, out uint version))
                {
                    entities.Add(new Entity(id, version));
                }
            }
            return entities.ToArray();
        }
    }
}
