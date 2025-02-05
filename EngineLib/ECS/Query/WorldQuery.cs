namespace EngineLib
{
    // Добавляем в World методы для работы с запросами
    public partial class World
    {
        private readonly List<Query> _activeQueries = new();
        private readonly object _queriesLock = new object();

        public Query CreateQuery()
        {
            var query = new Query(this);
            lock (_queriesLock)
            {
                _activeQueries.Add(query);
            }
            return query;
        }

        private void InvalidateQueries()
        {
            lock (_queriesLock)
            {
                foreach (var query in _activeQueries)
                {
                    query.InvalidateCache();
                }
            }
        }

        public bool HasComponent(ref Entity entity, Type componentType)
        {
            if (!IsEntityValid(ref entity))
                return false;

            if (!componentType.IsValueType || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Type {componentType} must be a struct implementing IComponent");
            }

            return _componentPool.HasComponentOfType(entity.Id, componentType);
        }

        internal IEnumerable<Entity> GetEntitiesWith(Type componentType)
        {
            if (!componentType.IsValueType || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Type {componentType} must be a struct implementing IComponent");
            }

            // Получаем все entityId для данного типа компонента напрямую из ComponentPool
            var entityIds = _componentPool.GetAllEntitiesWithType(componentType);

            foreach (var entityId in entityIds)
            {
                if (_entityVersions.TryGetValue(entityId, out var version))
                {
                    yield return new Entity(entityId, version);
                }
            }
        }

        public void CleanupUnusedQueries()
        {
            lock (_queriesLock)
            {
                _activeQueries.RemoveAll(q => q == null);
            }
        }

        // Вспомогательные методы для получения архетипов
        public ReadOnlySpan<uint> GetArchetypeEntities<T>()
            where T : struct, IComponent
        {
            return _archetypePool.GetEntitiesWith<T>();
        }

        public ReadOnlySpan<uint> GetArchetypeEntities<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            return _archetypePool.GetEntitiesWith<T1, T2>();
        }

        public ReadOnlySpan<uint> GetArchetypeEntities<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            return _archetypePool.GetEntitiesWith<T1, T2, T3>();
        }

        public ReadOnlySpan<uint> GetArchetypeEntities<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            return _archetypePool.GetEntitiesWith<T1, T2, T3, T4>();
        }

        // Методы для получения отфильтрованных сущностей через Query
        public List<Entity> GetEntitiesWith<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            return CreateQuery()
                .With<T1>()
                .With<T2>()
                .Build();
        }

        public List<Entity> GetEntitiesWith<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            return CreateQuery()
                .With<T1>()
                .With<T2>()
                .With<T3>()
                .Build();
        }

        public List<Entity> GetEntitiesWith<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            return CreateQuery()
                .With<T1>()
                .With<T2>()
                .With<T3>()
                .With<T4>()
                .Build();
        }
    }


}
