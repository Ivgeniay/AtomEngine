

namespace EngineLib
{
    public class Query
    {
        private readonly HashSet<Type> _requiredComponents = new();
        private readonly HashSet<Type> _excludedComponents = new();
        private readonly List<Func<Entity, bool>> _filters = new();
        private readonly World _world;
        private IEnumerable<Entity>? _cachedResult;
        private bool _isDirty = true;
        private int? _limit;
        private Func<Entity, object>? _orderBySelector;
        private bool _orderDescending;
        private Func<Entity, object>? _groupBySelector;

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

        public Query OrderBy<TKey>(Func<Entity, TKey> keySelector)
        {
            _orderBySelector = e => keySelector(e)!;
            _orderDescending = false;
            _isDirty = true;
            return this;
        }

        public Query OrderByDescending<TKey>(Func<Entity, TKey> keySelector)
        {
            _orderBySelector = e => keySelector(e)!;
            _orderDescending = true;
            _isDirty = true;
            return this;
        }

        public Query GroupBy<TKey>(Func<Entity, TKey> keySelector)
        {
            _groupBySelector = e => keySelector(e)!;
            _isDirty = true;
            return this;
        }

        public Query Where<T>(Func<T, bool> predicate) where T : struct, IComponent
        {
            _filters.Add(entity =>
            {
                //var componentOption = _world.GetComponentOrNone<T>(entity);
                //return componentOption.HasValue && predicate(componentOption.Value);
                Option<T> mb_componentOption = _world.GetComponentOrNone<T>(entity);
                T componentOption = mb_componentOption.Unwrap();
                return predicate(componentOption);
            });
            _isDirty = true;
            return this;
        }

        public IEnumerable<Entity> Build()
        {
            if (!_isDirty && _cachedResult != null)
                return _cachedResult;

            if (_requiredComponents.Count == 0)
            {
                _cachedResult = Enumerable.Empty<Entity>();
                return _cachedResult;
            }

            // Базовая фильтрация
            var query = FilterEntities();

            // Применяем сортировку
            if (_orderBySelector != null)
            {
                query = _orderDescending
                    ? query.OrderByDescending(_orderBySelector)
                    : query.OrderBy(_orderBySelector);
            }

            // Применяем лимит
            if (_limit.HasValue)
            {
                query = query.Take(_limit.Value);
            }

            // Кэшируем и возвращаем результат
            _cachedResult = query.ToList();
            _isDirty = false;
            return _cachedResult;
        }

        public IEnumerable<IGrouping<object, Entity>> BuildGrouped()
        {
            if (_groupBySelector == null)
            {
                throw new InvalidOperationException("GroupBy selector not specified");
            }

            var entities = Build();
            return entities.GroupBy(_groupBySelector);
        }

        private IEnumerable<Entity> FilterEntities()
        {
            var firstType = _requiredComponents.First();
            return _world.GetEntitiesWith(firstType).Where(entity =>
            {
                // Проверяем наличие всех требуемых компонентов
                foreach (var type in _requiredComponents.Skip(1))
                {
                    if (!_world.HasComponent(entity, type))
                        return false;
                }

                // Проверяем отсутствие исключенных компонентов
                foreach (var type in _excludedComponents)
                {
                    if (_world.HasComponent(entity, type))
                        return false;
                }

                // Применяем все фильтры
                return _filters.All(filter => filter(entity));
            });
        }

        // Сброс кэша
        internal void InvalidateCache()
        {
            _isDirty = true;
            _cachedResult = null;
        }
    }
}
