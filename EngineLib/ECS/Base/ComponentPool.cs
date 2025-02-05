using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace EngineLib
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

        private unsafe ref T GetComponentRef<T>(uint entityId) where T : struct, IComponent
        {
            var type = typeof(T);
            var components = _components[type];
            fixed (void* ptr = &Unsafe.Unbox<T>(components[entityId]))
            {
                return ref Unsafe.AsRef<T>(ptr);
            }
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

        public bool TryGetComponent<T>(uint entityId, out T component) where T : struct, IComponent
        {
            component = default;
            var type = typeof(T);

            if (!_components.TryGetValue(type, out var components) ||
                !components.TryGetValue(entityId, out var comp))
                return false;

            component = (T)comp;
            return true;
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

        public IReadOnlyDictionary<Type, ConcurrentDictionary<uint, IComponent>> GetAllComponentTypes()
        {
            return _components;
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

        public void Dispose()
        {
            if (_isDisposed) return;
            _components.Clear();
            _isDisposed = true;
        }
    }
}