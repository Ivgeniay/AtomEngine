using System.Collections.Concurrent;

namespace EngineLib
{
    public partial class World
    {
        private uint _nextEntityId = 0;
        private readonly List<System> _systems = new();
        private readonly SystemDependencyGraph _dependencyGraph = new();
        private readonly ResourceManager _resourceManager = new ResourceManager();
        private readonly ConcurrentDictionary<uint, uint> _entityVersions = new();
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<uint, IComponent>> _components = new();

        public Entity CreateEntity()
        {
            var id = Interlocked.Increment(ref _nextEntityId) - 1;
            _entityVersions.TryAdd(id, 0);
            return new Entity(id, 0);
        }
        public void AddComponent<T>(Entity entity, T component) where T : struct, IComponent
        {
            var type = typeof(T);
            var components = _components.GetOrAdd(type, _ => new ConcurrentDictionary<uint, IComponent>());
            components[entity.Id] = component;

            if (component is IDisposable disposable)
            {
                _resourceManager.RegisterResource(entity, disposable);
            }

            InvalidateQueries();
        }
        public Option<T> GetComponentOrNone<T>(Entity entity) where T : struct, IComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components) &&
                components.TryGetValue(entity.Id, out var component))
            {
                return Option<T>.Some((T)component);
            }
            return Option<T>.None();
        }
        public Result<T, ComponentError> GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            var type = typeof(T);
            if (!_components.TryGetValue(type, out var components))
                return Result<T, ComponentError>.Err(new ComponentError($"Component type {type} not found"));

            if (!components.TryGetValue(entity.Id, out var component))
                return Result<T, ComponentError>.Err(new ComponentError($"Component not found for entity {entity.Id}"));

            return Result<T, ComponentError>.Ok((T)component);
        }
        public Result<bool, ComponentError> RemoveComponent<T>(Entity entity) where T : struct, IComponent
        {
            if (!IsEntityValid(entity))
            {
                return Result<bool, ComponentError>.Err(
                    new ComponentError($"Entity {entity.Id} is not valid"));
            }

            var type = typeof(T);

            if (!_components.TryGetValue(type, out var components))
            {
                return Result<bool, ComponentError>.Err(
                    new ComponentError($"Component type {type.Name} not registered"));
            }
            if (!components.TryRemove(entity.Id, out var component))
            {
                return Result<bool, ComponentError>.Err(
                    new ComponentError($"Entity {entity.Id} doesn't have component {type.Name}"));
            }

            if (component is IDisposable disposable)
            {
                _resourceManager.CleanupResource(entity, disposable);
            }

            InvalidateQueries();
            return Result<bool, ComponentError>.Ok(true);
        }


        public void AddSystem(System system)
        {
            _systems.Add(system);
        }
        public void AddSystemDependency(System dependent, System dependency)
        {
            _dependencyGraph.AddDependency(dependent, dependency);
        }

        public async Task UpdateAsync(float deltaTime)
        {
            var systemLevels = _dependencyGraph.GetExecutionLevels();

            foreach (var level in systemLevels)
            {
                var systemTasks = level.Select(system =>
                    Task.Run(() => system.Update(deltaTime))
                );
                await Task.WhenAll(systemTasks);
            }
        }
        public void Update(float deltaTime)
        {
            UpdateAsync(deltaTime).GetAwaiter().GetResult();
        }

        public IEnumerable<Entity> GetEntitiesWith<T>() where T : IComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components))
            {
                foreach (var entityId in components.Keys)
                {
                    yield return new Entity(entityId, _entityVersions[entityId]);
                }
            }
        }

        public void DestroyEntity(Entity entity)
        {
            _resourceManager.CleanupEntity(entity);

            foreach (var components in _components.Values)
            {
                components.TryRemove(entity.Id, out _);
            }
            _entityVersions.AddOrUpdate(entity.Id, 1, (_, v) => v + 1);
            InvalidateQueries();
        }
        public bool IsEntityValid(Entity entity)
        {
            return _entityVersions.TryGetValue(entity.Id, out uint version) &&
                   version == entity.Version;
        }
    }
}
