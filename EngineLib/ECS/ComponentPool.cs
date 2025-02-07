using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AtomEngine
{
    public class ComponentPool : IDisposable
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<uint, IComponent>> _components = new();
        private bool _isDisposed;

        public ref T AddComponent<T>(uint entityId, in T component) where T : struct, IComponent
        {
            var type = typeof(T);
            var components = _components.GetOrAdd(type, _ => new ConcurrentDictionary<uint, IComponent>());
            components[entityId] = component;
            return ref GetComponentRef<T>(entityId);
        }

        public ref T GetComponent<T>(uint entityId) where T : struct, IComponent
        {
            var type = typeof(T);
            if (!_components.TryGetValue(type, out var components))
                throw new ComponentError($"Component type {type} not found");

            if (!components.TryGetValue(entityId, out var component))
                throw new ComponentError($"Component not found for entity {entityId}");

            return ref GetComponentRef<T>(entityId);
        }

        internal IEnumerable<uint> GetAllEntitiesWithType(Type componentType)
        {
            if (_components.TryGetValue(componentType, out var components))
            {
                return components.Keys;
            }
            return Enumerable.Empty<uint>();
        }

        public void RemoveComponent<T>(uint entityId) where T : struct, IComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components))
            {
                components.TryRemove(entityId, out _);
            }
        }

        public IReadOnlyDictionary<Type, ConcurrentDictionary<uint, IComponent>> GetAllComponentTypes() =>
            _components;

        public IEnumerable<Type> GetAllEntityComponentTypes(uint entity_id)
        {
            foreach (var componentDict in _components)
            {
                if (componentDict.Value.ContainsKey(entity_id))
                {
                    yield return componentDict.Key;
                }
            }
        }

        public bool TryGetComponentByType(uint entityId, Type componentType, out IComponent component)
        {
            component = null;
            return _components.TryGetValue(componentType, out var components) &&
                   components.TryGetValue(entityId, out component);
        }

        public void DestroyEntityComponents(uint entityId)
        {
            foreach (var components in _components.Values)
            {
                components.TryRemove(entityId, out _);
            }
        }

        public bool HasComponent<T>(uint entityId) where T : struct, IComponent
        {
            var type = typeof(T);
            return _components.TryGetValue(type, out var components) &&
                   components.ContainsKey(entityId);
        }

        internal bool HasComponentOfType(uint entityId, Type type)
        {
            return _components.TryGetValue(type, out var components) &&
                   components.ContainsKey(entityId);
        }

        private unsafe ref T GetComponentRef<T>(uint entityId) where T : struct, IComponent
        {
            var type = typeof(T);
            var components = _components[type];
            return ref Unsafe.Unbox<T>(components[entityId]);

            //fixed (void* ptr = &Unsafe.Unbox<T>(components[entityId]))
            //{
            //    return ref Unsafe.AsRef<T>(ptr);
            //}
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _components.Clear();
            _isDisposed = true;
        }
    }
}