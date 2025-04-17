using System.Collections.Concurrent;
using System.Numerics;

namespace AtomEngine
{
    public partial class World : IWorld, IDisposable
    {
        // ENTITY
        private readonly ConcurrentDictionary<uint, uint> _entityVersions = new ConcurrentDictionary<uint, uint>();
        private readonly Queue<uint> _recycledIds = new Queue<uint>();
        private readonly object _entityCreationLock = new object();
        private uint _nextEntityId = 0;
        // SYSTEMS
        private readonly SystemDependencyGraph _dependencyGraph = new SystemDependencyGraph();
        private readonly List<ISystem> _systems = new List<ISystem>();
        private readonly List<ISystem> _initialize_systems = new List<ISystem>();
        private readonly List<IRenderSystem> _renderSystems = new List<IRenderSystem>();
        private readonly List<IRenderSystem> initialize_render_systems = new List<IRenderSystem>();
        private readonly List<IPhysicSystem> _physicSystems = new List<IPhysicSystem>();
        private readonly object _systemsLock = new object();
        private readonly object _renderSystemsLock = new object();

        private readonly ConcurrentDictionary<ISystem, int> _systemExecutionOrder = new ConcurrentDictionary<ISystem, int>();
        private readonly ConcurrentDictionary<IRenderSystem, int> _renderSystemExecutionOrder = new ConcurrentDictionary<IRenderSystem, int>();
        private readonly ConcurrentDictionary<IPhysicSystem, int> _physicSystemExecutionOrder = new ConcurrentDictionary<IPhysicSystem, int>();
        // COMPONENTS
        private readonly ConcurrentDictionary<uint, Archetype> _entityArchetypesCache = new ConcurrentDictionary<uint, Archetype>();
        private readonly ResourceManager _resourceManager = new ResourceManager();
        private readonly ComponentPool _componentPool = new ComponentPool();
        private readonly ArchetypePool _archetypePool = new ArchetypePool();
        // PHYSICS
        public readonly BVHPool BvhPool;

        private bool _isDisposed;
        private WorldAdmin worldAdmin;
        public World()
        {
            BvhPool = new BVHPool(this);
            worldAdmin = new WorldAdmin(this);
        }
        public WorldAdmin GetAdmin() => worldAdmin;

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

        public void DestroyEntity(uint entityId, uint entityVersion) => DestroyEntity(new Entity(entityId, entityVersion));
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
        public ref T GetAbstractComponent<T>(Entity entity) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                throw new ArgumentException($"Entity {entity} is not valid");

            return ref _componentPool.GetComponent<T>(entity.Id);
        }
        public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
        {
            if (!IsEntityValid(entity.Id, entity.Version))
                throw new ArgumentException($"Entity {entity} is not valid");

            return ref _componentPool.GetComponent<T>(entity.Id);
        }
        public void ModifyComponent(uint entityId, Type type, Func<IComponent, IComponent> func) =>
            _componentPool.ModifyComponent(entityId, type, func);
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


            ref var addedComponent = ref _componentPool.AddComponent(entity.Id, component);
            //_entityArchetypesCache.TryRemove(entity.Id, out _);

            if (component is IDisposable disposable)
            {
                _resourceManager.RegisterResource(entity, disposable);
            }

            //Archetype currentArchetype = GetEntityArchetype(ref entity);
            //Type[] componentTypes = currentArchetype.Metadata
            //    .Select(m => m.Type)
            //    .Append(typeof(T))
            //    .Distinct()
            //    .ToArray();

            //ReadOnlySpan<IComponent> components = GatherComponents(ref entity, componentTypes);
            //_archetypePool.AddEntityToArchetype(entity.Id, components, componentTypes);

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

            if (component is IDisposable disposable)
            {
                _resourceManager.CleanupResource(entity, disposable);
            }

            _componentPool.RemoveComponent<T>(entity.Id);
            _entityArchetypesCache.TryRemove(entity.Id, out _);

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

            //if (typeof(T) == typeof(BoundingComponent))
            //{
            //    BvhPool.RemoveEntity(entity);
            //}

            InvalidateQueries();
        }
        public void WithComponent(uint entityId, Type componentType, Action<object> action)
        {
            _componentPool.WithComponent(entityId, componentType, action);
        }

        public Archetype GetEntityArchetype(ref Entity entity)
        {
            if (_entityArchetypesCache.TryGetValue(entity.Id, out var cachedArchetype))
            {
                return cachedArchetype;
            }

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
        public void AddSystem(ICommonSystem system)
        {
            if (system == null) throw new NullValueError(nameof(system));

            if (system is IPhysicSystem physicsSystem) AddSystem(physicsSystem);
            else if(system is IRenderSystem render) AddSystem(render);
            else if (system is ISystem sys) AddSystem(sys);
        }
        public void AddSystem(IRenderSystem system)
        {
            lock (_renderSystemsLock)
            {
                _renderSystems.Add(system);
                initialize_render_systems.Add(system);
                _renderSystemExecutionOrder[system] = -1;
            }
        }
        public void AddSystem(ISystem system)
        {
            lock (_systemsLock)
            {
                _systems.Add(system);
                _dependencyGraph.AddSystem(system);
                _initialize_systems.Add(system);
                if (system is IPhysicSystem physicSystem)
                {
                    _physicSystems.Add(physicSystem);
                    _physicSystemExecutionOrder[physicSystem] = -1;
                }
                else
                {
                    _systemExecutionOrder[system] = -1;
                }
            }
        }

        public void RemoveSystem(ISystem system)
        {
            lock (_systemsLock)
            {
                if (_systems.Contains(system))
                {
                    _systems.Remove(system);
                }
                _dependencyGraph.RemoveSystem(system);
                if (_initialize_systems.Contains(system))
                {
                    _initialize_systems.Remove(system);
                }
                if (system is IPhysicSystem physics)
                {
                    _physicSystems.Remove(physics);
                    _physicSystemExecutionOrder.TryRemove(physics, out _);
                }
                else
                {
                    _systemExecutionOrder.TryRemove(system, out _);
                }
            }
        }
        public void RemoveSystem(IRenderSystem system)
        {
            lock (_renderSystemsLock)
            {
                if (_renderSystems.Contains(system))
                {
                    _renderSystems.Remove(system);
                }
                _renderSystemExecutionOrder.TryRemove(system, out _);
            }
        }
        public void RemoveSystem(ICommonSystem system)
        {
            if (system == null) throw new NullValueError(nameof(system));

            if (system is IPhysicSystem physicsSystem)
            {
                RemoveSystem(physicsSystem);
            }
            else if (system is IRenderSystem render)
            {
                RemoveSystem(render);
            }
            else if (system is ISystem sys)
            {
                RemoveSystem(sys);
            }
        }

        public void SetOrder(ISystem system, int order)
        {
            lock (_systemsLock)
            {
                if (system is IPhysicSystem physic) SetOrder(physic, order);

                if (!_systems.Contains(system))
                    throw new ArgumentException("System must be added to the world first");

                _systemExecutionOrder[system] = order;
            }
        }
        public void SetOrder(IRenderSystem system, int order)
        {
            lock (_renderSystemsLock)
            {
                if (!_renderSystems.Contains(system))
                    throw new ArgumentException("Render system must be added to the world first");

                _renderSystemExecutionOrder[system] = order;
            }
        }
        public void SetOrder(IPhysicSystem system, int order)
        {
            lock (_systemsLock)
            {
                if (!_physicSystems.Contains(system))
                    throw new ArgumentException("Physics system must be added to the world first");

                _physicSystemExecutionOrder[system] = order;
            }
        }
        public void SetOrder(ICommonSystem system, int order)
        {
            if (system == null) throw new NullValueError(nameof(system));

            if (system is IPhysicSystem physicsSystem)
            {
                SetOrder(physicsSystem, order);
            }
            else if (system is IRenderSystem render)
            {
                SetOrder(render, order);
            }
            else if (system is ISystem sys)
            {
                SetOrder(sys, order);
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
                var systemGroups = level
                    .GroupBy(system => {
                        if (!_systemExecutionOrder.TryGetValue(system, out int order))
                            order = -1;
                        return order;
                    })
                    .OrderBy(g => g.Key == -1 ? int.MaxValue : g.Key);

                foreach (var group in systemGroups)
                {
                    var systemTasks = group.Select(system =>
                        Task.Run(() => UpdateSystemSafely(system, deltaTime))
                    );
                    await Task.WhenAll(systemTasks);
                }
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
            if (_initialize_systems.Count > 0)
            {
                foreach (var system in _initialize_systems)
                {
                    try
                    {
                        system.Initialize();
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Initializing {system.GetType().Name} was crashed {ex}");
                    }
                }
                _initialize_systems.Clear();
            }
            UpdateAsync(deltaTime).GetAwaiter().GetResult();
        }

        public void UpdateSingeThread(double deltaTime)
        {
            if (_initialize_systems.Count > 0)
            {
                foreach (var system in _initialize_systems)
                {
                    try
                    {
                        system.Initialize();
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Initializing {system.GetType().Name} was crashed {ex}");
                    }
                }
                _initialize_systems.Clear();
            }

            var systemLevels = _dependencyGraph.GetExecutionLevels();
            foreach (var level in systemLevels)
            {
                var sortedSystems = level
                        .OrderBy(system => {
                            if (!_systemExecutionOrder.TryGetValue(system, out int order))
                                order = -1;
                            return order == -1 ? int.MaxValue : order;
                        })
                        .ToList();

                foreach (var system in sortedSystems)
                {
                    UpdateSystemSafely(system, deltaTime);
                }
            }
        }

        public void FixedUpdate()
        {
            var sortedPhysicSystems = _physicSystems
                    .OrderBy(system => {
                        if (!_physicSystemExecutionOrder.TryGetValue(system, out int order))
                            order = -1;
                        return order == -1 ? int.MaxValue : order;
                    })
                    .ToList();

            foreach (var system in sortedPhysicSystems)
            {
                try
                {
                    system.FixedUpdate();
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"FixedUpadete of {system.GetType().Name} was crashed {ex}");
                }
            }
        }

        public void Render(double deltaTime, object? context)
        {
            if (initialize_render_systems.Count > 0)
            {
                foreach (var system in initialize_render_systems)
                {
                    try
                    {
                        system.Initialize();
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Initializing {system.GetType().Name} was crashed {ex}");
                    }
                }
                initialize_render_systems.Clear();
            }

            var sortedRenderSystems = _renderSystems
                .OrderBy(system => {
                    if (!_renderSystemExecutionOrder.TryGetValue(system, out int order))
                        order = -1;
                    return order == -1 ? int.MaxValue : order;
                })
                .ToList();

            foreach (var system in sortedRenderSystems)
            {
                try
                {
                    system.Render(deltaTime, context);
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Render {system.GetType().Name} was crashed {ex}");
                }

            }
        }
        public void Resize(Vector2 size)
        {
            foreach (var system in _renderSystems)
                system.Resize(size);
        }


        #region Physics

        #endregion

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

        public class WorldAdmin
        {
            private readonly World world;
            public WorldAdmin(World world)
            {
                this.world = world;
            }

            public Entity CreateEntityWithId(uint id, uint version)
            {
                lock (world._entityCreationLock)
                {
                    world._entityVersions[id] = version;
                    if (id >= world._nextEntityId)
                    {
                        world._nextEntityId = id + 1;
                    }

                    return new Entity(id, version);
                }
            }
        }
    }
}
