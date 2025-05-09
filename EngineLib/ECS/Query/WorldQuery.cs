﻿namespace AtomEngine
{
    public partial class World
    {
        private readonly List<QueryEntity> _activeQueries = new();
        private readonly object _queriesLock = new object();

        public QueryEntity CreateEntityQuery()
        {
            var query = new QueryEntity(this);
            lock (_queriesLock)
            {
                _activeQueries.Add(query);
            }
            WeakReference weakReference = new WeakReference(query);
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

        internal IEnumerable<Entity> QueryEntities(Type componentType)
        {
            if (!componentType.IsValueType || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Type {componentType} must be a struct implementing IComponent");
            }

            var entityIds = _componentPool.GetAllEntitiesWithType(componentType);

            foreach (var entityId in entityIds)
            {
                if (_entityVersions.TryGetValue(entityId, out var version))
                {
                    yield return new Entity(entityId, version);
                }
            }
        } 

        internal void CleanupUnusedQueries()
        {
            lock (_queriesLock)
            {
                _activeQueries.RemoveAll(q => q == null);
            }
        }
    }


}
