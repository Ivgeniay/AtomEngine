﻿using System.Collections.Concurrent;
using System.Numerics;

namespace AtomEngine
{
    //// TODO: Реализовать мультизависимость систем 
    
    public partial class World : IWorld, IDisposable
    {
        // ENTITY
        private readonly ConcurrentDictionary<uint, uint> _entityVersions = new();
        private readonly Queue<uint> _recycledIds = new();
        private readonly object _entityCreationLock = new();
        private uint _nextEntityId = 0;
        // SYSTEMS
        private readonly SystemDependencyGraph _dependencyGraph = new();
        private readonly List<ISystem> _systems = new();
        private readonly List<ISystem> initialize_systems = new();
        private readonly List<IRenderSystem> _renderSystems = new();
        private readonly List<IRenderSystem> initialize_render_systems = new();
        private readonly object _systemsLock = new();
        private readonly object _renderSystemsLock = new();
        // COMPONENTS
        private readonly ConcurrentDictionary<uint, Archetype> _entityArchetypesCache = new();
        private readonly ResourceManager _resourceManager = new ResourceManager();
        private readonly ComponentPool _componentPool = new();
        private readonly ArchetypePool _archetypePool = new();

        private bool _isDisposed;

        #region Entity
        public Entity CreateEntity()
        {
            lock (_entityCreationLock)
            {
                uint id;
                uint version;

                if (_recycledIds.TryDequeue(out id))
                {
                    version = _entityVersions.GetOrAdd(id, 0);
                    return new Entity(id, version);
                }

                id = Interlocked.Increment(ref _nextEntityId) - 1;
                version = _entityVersions.GetOrAdd(id, 0);
                return new Entity(id, version);
            }
        }
        public void DestroyEntity(Entity entity)
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                return;

            _resourceManager.CleanupEntity(ref entity);
            _componentPool.DestroyEntityComponents(entity.Id);
            _archetypePool.RemoveEntity(entity.Id);
            _entityVersions.AddOrUpdate(entity.Id, 1, (_, v) => v + 1);
            lock (_entityCreationLock)
            {
                _recycledIds.Enqueue(entity.Id);
            }
            _entityArchetypesCache.TryRemove(entity.Id, out _);
            InvalidateQueries();
        }
        public bool IsEntityValid(uint entity_id, uint _version)
        {
            return _entityVersions.TryGetValue(entity_id, out uint currentVersion) &&
                   currentVersion == _version;
        }
        #endregion

        #region Components
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
        public bool HasComponent(Entity entity, Type componentType)
        {
            if (!componentType.IsValueType || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Type {componentType} must be a struct implementing IComponent");
            }

            return _componentPool.HasComponentOfType(entity.Id, componentType);
        }
        public ref T AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                throw new ArgumentException($"Entity {entity.Id} is not valid");

            // Добавляем в ComponentPool
            ref var addedComponent = ref _componentPool.AddComponent(entity.Id, component);
            _entityArchetypesCache.TryRemove(entity.Id, out _);

            // Если компонент реализует IDisposable, регистрируем его в ResourceManager
            if (component is IDisposable disposable)
            {
                _resourceManager.RegisterResource(entity, disposable);
            }

            // Добавляем в ArchetypePool, если нужно
            Archetype currentArchetype = GetEntityArchetype(ref entity);
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
            _entityArchetypesCache.TryRemove(entity.Id, out _);

            // Обновляем архетип
            var currentArchetype = GetEntityArchetype(ref entity);
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
        
        public Archetype GetEntityArchetype(ref Entity entity)
        {
            // Пробуем получить из кэша
            if (_entityArchetypesCache.TryGetValue(entity.Id, out var cachedArchetype))
            {
                return cachedArchetype;
            }

            // Собираем типы всех компонентов сущности
            IEnumerable<Type> componentTypes = _componentPool.GetAllEntityComponentTypes(entity.Id);

            var archetype = new Archetype(componentTypes.ToArray());
            _entityArchetypesCache[entity.Id] = archetype;
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
        #endregion

        #region Systems
        public void AddSystem(IRenderSystem system)
        {
            lock (_renderSystemsLock)
            {
                _renderSystems.Add(system);
                initialize_render_systems.Add(system);
            }
        }
        public void AddSystem(ISystem system)
        {
            lock (_systemsLock)
            {
                _systems.Add(system);
                _dependencyGraph.AddSystem(system);
                initialize_systems.Add(system);
            }
        }
        public void AddSystemDependency(ISystem dependent, ISystem dependency)
        {
            lock (_systemsLock)
            {
                if (!_systems.Contains(dependent) || !_systems.Contains(dependency))
                    throw new ArgumentException("Both systems must be added to the world first");

                _dependencyGraph.AddDependency(dependent, dependency);
            }
        }
        #endregion

        private async Task UpdateAsync(double deltaTime)
        {
            var systemLevels = _dependencyGraph.GetExecutionLevels();
            foreach (var level in systemLevels)
            {
                var systemTasks = level.Select(system =>
                    Task.Run(() => UpdateSystemSafely(system, deltaTime))
                );
                await Task.WhenAll(systemTasks);
            }
        }
        private void UpdateSystemSafely(ISystem system, double deltaTime)
        {
            try
            {
                system.Update(deltaTime);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error updating system {system.GetType().Name}: {ex}");
            }
        }
        public void Update(double deltaTime)
        {
            if (initialize_systems.Count > 0)
            {
                foreach (var system in initialize_systems)
                {
                    system.Initialize();
                }
                initialize_systems.Clear();
            }
            UpdateAsync(deltaTime).GetAwaiter().GetResult();
            //var systemLevels = _dependencyGraph.GetExecutionLevels();
            //foreach (var level in systemLevels)
            //{
            //    level.ForEach(system => UpdateSystemSafely(system, deltaTime));
            //}
        }
        public void Render(double deltaTime)
        {
            if (initialize_render_systems.Count > 0)
            {
                foreach (var system in initialize_render_systems)
                {
                    system.Initialize();
                }
                initialize_render_systems.Clear();
            }
            foreach (var system in _renderSystems)
                system.Render(deltaTime);
        }
        public void Resize(Vector2 size)
        {
            foreach (var system in _renderSystems)
                system.Resize(size);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _componentPool.Dispose();
            _archetypePool.Dispose();
            foreach (var system in _systems)
            {
                if (system is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            foreach (var system in _renderSystems)
            {
                if (system is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _isDisposed = true;
        }
    }
}
