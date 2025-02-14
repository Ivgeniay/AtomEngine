using System.Collections.Concurrent;
using System.ComponentModel;
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
            //if (type.IsInterface) return ref GetComponentRef<T>(entityId);

            if (!_components.TryGetValue(type, out var components))
                throw new ComponentError($"Component type {type} not found");

            if (!components.TryGetValue(entityId, out var component))
                throw new ComponentError($"Component {type.Name} not found for entity {entityId}");

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
            return HasComponentOfType(entityId, typeof(T));
        }

        public IEnumerable<T> GetComponentsByType<T>() where T : struct, IComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var components))
            {
                yield return (T)components.Values;
            } 
        } 
        internal bool HasComponentOfType(uint entityId, Type type)
        {
            //var matchingType = FindMatchingGenericType(type);
            var matchingType = type;
            if (matchingType == null)
                return false;

            return _components.TryGetValue(matchingType, out var components) &&
                   components.ContainsKey(entityId);
        }

        private unsafe ref T GetComponentRef<T>(uint entityId) where T : struct, IComponent
        {
            var matchingType = typeof(T);

            //if (matchingType.IsInterface)
            //{
            //    foreach (var (componentType, _components) in _components)
            //    {
            //        if (matchingType.IsAssignableFrom(componentType) &&
            //            _components.TryGetValue(entityId, out var _component))
            //        {
            //            return ref Unsafe.Unbox<T>(_component);
            //        }
            //    }
            //    throw new KeyNotFoundError($"No component implementing {matchingType} found for entity {entityId}");
            //}

            var components = _components[matchingType];
            var component = components[entityId];

            return ref Unsafe.Unbox<T>(component);
        }


        /// <summary>
        /// Ищет дженерик тип, который соответствует запрошенному абстрактному типу компонента
        /// Ищет соответствия где A : IB
        /// TypeComp<IB> -> TypeComp<A>
        /// </summary>
        /// <param name="requestedType"></param>
        /// <returns></returns>
        private Type FindMatchingGenericType(Type requestedType)
        {
            if (!requestedType.IsGenericType)
                return requestedType;

            var requestedGenericDef = requestedType.GetGenericTypeDefinition();
            var requestedGenericArgs = requestedType.GetGenericArguments();

            foreach (var storedType in _components.Keys)
            {
                if (!storedType.IsGenericType)
                    continue;

                var storedGenericDef = storedType.GetGenericTypeDefinition();

                if (storedGenericDef != requestedGenericDef)
                    continue;
                var storedGenericArgs = storedType.GetGenericArguments();
                if (storedGenericArgs.Length != requestedGenericArgs.Length)
                    continue;

                bool isMatch = true;
                for (int i = 0; i < requestedGenericArgs.Length; i++)
                {
                    var requestedArg = requestedGenericArgs[i];
                    var storedArg = storedGenericArgs[i];

                    if (requestedArg.IsInterface)
                    {
                        if (!requestedArg.IsAssignableFrom(storedArg))
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    else if (requestedArg != storedArg)
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                    return storedType;
            }

            return null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _components.Clear();
            _isDisposed = true;
        }
    }
}