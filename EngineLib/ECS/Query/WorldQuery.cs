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


        public bool HasComponent(Entity entity, Type componentType)
        {
            if (!componentType.IsValueType || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Type {componentType} must be a struct implementing IComponent");
            }

            return _components.TryGetValue(componentType, out var components) &&
                   components.ContainsKey(entity.Id);
        }
        public bool HasComponent<T>(Entity entity) where T : struct, IComponent
        {
            return HasComponent(entity, typeof(T));
        }


        public IEnumerable<Entity> GetEntitiesWith<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            return CreateQuery()
                .With<T1>()
                .With<T2>()
                .Build();
        }
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3>()
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
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3, T4>()
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
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            return CreateQuery()
                .With<T1>()
                .With<T2>()
                .With<T3>()
                .With<T4>()
                .With<T5>()
                .Build();
        }
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            return CreateQuery()
                .With<T1>()
                .With<T2>()
                .With<T3>()
                .With<T4>()
                .With<T5>()
                .With<T6>()
                .Build();
        }

        internal IEnumerable<Entity> GetEntitiesWith(Type componentType)
        {
            if (!componentType.IsValueType || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Type {componentType} must be a struct implementing IComponent");
            }

            if (_components.TryGetValue(componentType, out var components))
            {
                foreach (var entityId in components.Keys)
                {
                    if (_entityVersions.TryGetValue(entityId, out var version))
                    {
                        yield return new Entity(entityId, version);
                    }
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
    }
}
