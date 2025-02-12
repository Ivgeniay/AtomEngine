namespace AtomEngine
{
    public class QueryEntity
    {
        public delegate bool QueryFilter(Entity entity);
        public delegate TResult QuerySelector<TResult>(Entity entity);

        private readonly HashSet<Type> _requiredComponents = new();
        private readonly HashSet<Type> _excludedComponents = new();
        private readonly List<QueryFilter> _filters = new();
        private readonly World _world;
        private IEnumerable<Entity>? _cachedResult;
        private bool _isDirty = true;
        private int? _limit;
        private QuerySelector<IComparable>? _orderBySelector;
        private bool _orderDescending;

        internal QueryEntity(World world)
        {
            _world = world;
        }

        public QueryEntity With<T>() where T : struct, IComponent
        {
            _requiredComponents.Add(typeof(T));
            _isDirty = true;
            return this;
        }

        public QueryEntity Without<T>() where T : struct, IComponent
        {
            _excludedComponents.Add(typeof(T));
            _isDirty = true;
            return this;
        }

        public QueryEntity Limit(int count)
        {
            _limit = count;
            _isDirty = true;
            return this;
        }

        public QueryEntity OrderBy<TKey>(QuerySelector<TKey> keySelector) where TKey : IComparable
        {
            _orderBySelector = e => keySelector(e);
            _orderDescending = false;
            _isDirty = true;
            return this;
        }

        public QueryEntity OrderByDescending<TKey>(QuerySelector<TKey> keySelector) where TKey : IComparable
        {
            _orderBySelector = e => keySelector(e);
            _orderDescending = true;
            _isDirty = true;
            return this;
        }

        public QueryEntity Where(QueryFilter filter)
        {
            _filters.Add(filter);
            _isDirty = true;
            return this;
        }

        public QueryEntity Where<T>(Func<T, bool> predicate) where T : struct, IComponent
        {
            _filters.Add(FilterComponent);

            bool FilterComponent(Entity entity)
            {
                try
                {
                    var component = _world.GetComponent<T>(entity);
                    return predicate(component);
                }
                catch
                {
                    return false;
                }
            }

            _isDirty = true;
            return this;
        }

        public Entity[] Build()
        {
            if (!_isDirty && _cachedResult != null)
                return _cachedResult.ToArray();

            if (_requiredComponents.Count == 0)
            {
                _cachedResult = Array.Empty<Entity>();
                return _cachedResult.ToArray();
            }

            IEnumerable<Entity> results = FilterEntities();

            if (_orderBySelector != null)
            {
                results = _orderDescending
                    ? results.OrderByDescending(e => _orderBySelector(e))
                    : results.OrderBy(e => _orderBySelector(e));
            }

            if (_limit.HasValue && results.Count() > _limit.Value)
            {
                results = results.Take(_limit.Value);
            }

            _cachedResult = results;
            _isDirty = false;
            return results.ToArray();
        }

        private IEnumerable<Entity> FilterEntities()
        {
            var firstType = _requiredComponents.First();
            foreach (Entity entity in _world.QueryEntities(firstType))
            {
                bool isValid = true;
                if (!_world.IsEntityValid(entity.Id, entity.Version))
                    isValid= false;

                if (!isValid) continue;

                // Проверяем наличие всех требуемых компонентов
                foreach (var type in _requiredComponents.Skip(1))
                {
                    if (!_world.HasComponent(entity, type))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid) continue;

                // Проверяем отсутствие исключенных компонентов
                foreach (var type in _excludedComponents)
                {
                    if (_world.HasComponent(entity, type))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid) continue;

                // Применяем все фильтры
                foreach (var filter in _filters)
                {
                    if (!filter(entity))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    yield return entity;
                }
            }
        }

        internal void InvalidateCache()
        {
            _isDirty = true;
            _cachedResult = null;
        }
    }
}
