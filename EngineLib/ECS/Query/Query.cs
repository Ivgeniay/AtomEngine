

namespace EngineLib
{
    public class Query
    {
        public delegate bool QueryFilter(ref Entity entity);
        public delegate TResult QuerySelector<TResult>(ref Entity entity);

        private readonly HashSet<Type> _requiredComponents = new();
        private readonly HashSet<Type> _excludedComponents = new();
        private readonly List<QueryFilter> _filters = new();
        private readonly World _world;
        private List<Entity>? _cachedResult;
        private bool _isDirty = true;
        private int? _limit;
        private QuerySelector<object>? _orderBySelector;
        private bool _orderDescending;

        internal Query(World world)
        {
            _world = world;
        }

        public Query With<T>() where T : struct, IComponent
        {
            _requiredComponents.Add(typeof(T));
            _isDirty = true;
            return this;
        }

        public Query Without<T>() where T : struct, IComponent
        {
            _excludedComponents.Add(typeof(T));
            _isDirty = true;
            return this;
        }

        public Query Limit(int count)
        {
            _limit = count;
            _isDirty = true;
            return this;
        }

        public Query OrderBy<TKey>(QuerySelector<TKey> keySelector)
        {
            _orderBySelector = (ref Entity e) => keySelector(ref e)!;
            _orderDescending = false;
            _isDirty = true;
            return this;
        }

        public Query OrderByDescending<TKey>(QuerySelector<TKey> keySelector)
        {
            _orderBySelector = (ref Entity e) => keySelector(ref e)!;
            _orderDescending = true;
            _isDirty = true;
            return this;
        }

        public Query Where<T>(Func<T, bool> predicate) where T : struct, IComponent
        {
            _filters.Add(FilterComponent);

            bool FilterComponent(ref Entity entity)
            {
                try
                {
                    ref var component = ref _world.GetComponent<T>(ref entity);
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

        public List<Entity> Build()
        {
            if (!_isDirty && _cachedResult != null)
                return _cachedResult;

            if (_requiredComponents.Count == 0)
            {
                _cachedResult = new List<Entity>();
                return _cachedResult;
            }

            // Базовая фильтрация
            var results = new List<Entity>();
            foreach (var entity in FilterEntities())
            {
                results.Add(entity);
            }

            // Применяем сортировку если есть
            if (_orderBySelector != null)
            {
                // Создаем список пар (entity, key) для сортировки
                var sortableList = new List<(Entity entity, object key)>(results.Count);
                foreach (var entity in results)
                {
                    var e = entity;
                    sortableList.Add((entity, _orderBySelector(ref e)));
                }

                // Сортируем
                if (_orderDescending)
                {
                    sortableList.Sort((a, b) => Comparer<object>.Default.Compare(b.key, a.key));
                }
                else
                {
                    sortableList.Sort((a, b) => Comparer<object>.Default.Compare(a.key, b.key));
                }

                // Обновляем результаты
                results = sortableList.Select(x => x.entity).ToList();
            }

            // Применяем лимит если есть
            if (_limit.HasValue && results.Count > _limit.Value)
            {
                results.RemoveRange(_limit.Value, results.Count - _limit.Value);
            }

            // Кэшируем и возвращаем результат
            _cachedResult = results;
            _isDirty = false;
            return results;
        }

        private IEnumerable<Entity> FilterEntities()
        {
            var firstType = _requiredComponents.First();
            var entities = _world.GetEntitiesWith(firstType);

            foreach (var entity in entities)
            {
                var e = entity;
                bool isValid = true;

                foreach (var type in _requiredComponents.Skip(1))
                {
                    if (!_world.HasComponent(ref e, type))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid) continue;
                foreach (var type in _excludedComponents)
                {
                    if (_world.HasComponent(ref e, type))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid) continue;
                foreach (var filter in _filters)
                {
                    if (!filter(ref e))
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
