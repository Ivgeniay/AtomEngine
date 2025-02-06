using System.Collections.Concurrent;

namespace EngineLib
{
    public partial class World : IDisposable
    {
        private uint _nextEntityId = 0;
        private readonly List<System> _systems = new();
        private readonly SystemDependencyGraph _dependencyGraph = new();
        private readonly ResourceManager _resourceManager = new ResourceManager();
        private readonly ConcurrentDictionary<uint, uint> _entityVersions = new();
        private readonly ComponentPool _componentPool = new();
        private readonly ArchetypePool _archetypePool = new();
        private readonly ConcurrentDictionary<uint, Archetype> _entityArchetypes = new();
        private bool _isDisposed;

        public Entity CreateEntity()
        {
            var id = Interlocked.Increment(ref _nextEntityId) - 1;
            _entityVersions.TryAdd(id, 0);
            return new Entity(id, 0);
        }
        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                throw new ArgumentException($"Entity {entity} is not valid");

            return ref _componentPool.GetComponent<T>(entity.Id);
        }
        public bool HasComponent<T>(Entity entity) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                return false;

            return _componentPool.HasComponent<T>(entity.Id);
        }
        public ref T AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                throw new ArgumentException($"Entity {entity.Id} is not valid");

            // Добавляем в ComponentPool
            ref var addedComponent = ref _componentPool.AddComponent(entity.Id, component);
            _entityArchetypes.TryRemove(entity.Id, out _);

            // Если компонент реализует IDisposable, регистрируем его в ResourceManager
            if (component is IDisposable disposable)
            {
                _resourceManager.RegisterResource(entity, disposable);
            }

            // Добавляем в ArchetypePool, если нужно
            Archetype currentArchetype = GetEntityArchetype(entity);
            Type[] componentTypes = currentArchetype.Metadata
                .Select(m => m.Type)
                .Append(typeof(T))
                .Distinct()
                .ToArray();

            ReadOnlySpan<IComponent> components = GatherComponents(ref entity, componentTypes);
            _archetypePool.AddEntityToArchetype(entity.Id, components, componentTypes);

            InvalidateQueries();
            return ref addedComponent;
        }
        public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                return;

            if (!_componentPool.HasComponent<T>(entity.Id))
                return;

            ref T component = ref _componentPool.GetComponent<T>(entity.Id);

            // Если компонент реализует IDisposable, очищаем ресурс
            if (component is IDisposable disposable)
            {
                _resourceManager.CleanupResource(entity, disposable);
            }

            _componentPool.RemoveComponent<T>(entity.Id);
            _entityArchetypes.TryRemove(entity.Id, out _);

            // Обновляем архетип
            var currentArchetype = GetEntityArchetype(entity);
            var componentTypes = currentArchetype.Metadata
                .Select(m => m.Type)
                .Where(t => t != typeof(T))
                .ToArray();

            if (componentTypes.Length > 0)
            {
                var components = GatherComponents(ref entity, componentTypes);
                _archetypePool.AddEntityToArchetype(entity.Id, components, componentTypes);
            }

            InvalidateQueries();
        }
        public void DestroyEntity(Entity entity)
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                return;

            _resourceManager.CleanupEntity(ref entity);
            _componentPool.DestroyEntityComponents(entity.Id);
            _archetypePool.RemoveEntity(entity.Id);
            _entityVersions.AddOrUpdate(entity.Id, 1, (_, v) => v + 1);
            _entityArchetypes.TryRemove(entity.Id, out _);

            InvalidateQueries();
        }
        public bool IsEntityValid(uint entity_id, uint _version)
        {
            return _entityVersions.TryGetValue(entity_id, out uint version) &&
                   version == _version;
        }

        private Archetype GetEntityArchetype(Entity entity)
        {
            // Пробуем получить из кэша
            if (_entityArchetypes.TryGetValue(entity.Id, out var cachedArchetype))
            {
                return cachedArchetype;
            }

            // Собираем типы всех компонентов сущности
            IEnumerable<Type> componentTypes = _componentPool.GetAllEntityComponentTypes(entity.Id);

            var archetype = new Archetype(componentTypes.ToArray());
            _entityArchetypes[entity.Id] = archetype;
            return archetype;
        }
        private ReadOnlySpan<IComponent> GatherComponents(ref Entity entity, Type[] types)
        {
            IComponent[] components = new IComponent[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                if (_componentPool.TryGetComponentByType(entity.Id, types[i], out var component))
                {
                    components[i] = component;
                }
            }

            return components;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _componentPool.Dispose();
            _archetypePool.Dispose();
            _isDisposed = true;
        }
    }
}
