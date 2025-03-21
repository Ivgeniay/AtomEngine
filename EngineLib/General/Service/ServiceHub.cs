using System.Collections.Concurrent;
using AtomEngine;

namespace EngineLib
{
    public static class ServiceHub
    {
        private static ConcurrentDictionary<Type, IService> services = new ConcurrentDictionary<Type, IService>();
        private static ConcurrentDictionary<Type, Type> typeMapping = new ConcurrentDictionary<Type, Type>();
        private static Queue<IService> _queueInitializingService = new Queue<IService>();
        public static int QuontityInInitOrder { get => _queueInitializingService.Count; }

        public static void RegisterService<T>() where T : class, IService, new()
        {
            T service = new T();
            services[typeof(T)] = service;
            _queueInitializingService.Enqueue(service);
        }

        public static async Task Initialize(
            Action<Type> OnStartInitializeCollback = null,
            Action<Type> OnInitializedCallback = null
            )
        {
            while (_queueInitializingService.Count > 0)
            {
                var service = _queueInitializingService.Dequeue();
                if (service != null)
                {
                    OnStartInitializeCollback?.Invoke(service.GetType());
                    await service.InitializeAsync();
                    OnInitializedCallback?.Invoke(service.GetType());
                }
            }
        }

        public static T Get<T>() where T : class, IService
        {
            Type type = typeof(T);

            if (services.TryGetValue(type, out IService service))
            {
                return (T)service;
            }

            if (typeMapping.TryGetValue(type, out Type mappedType))
            {
                if (services.TryGetValue(mappedType, out IService mappedService))
                {
                    return (T)mappedService;
                }
            }

            DebLogger.Error($"There is no {type} in the service hub");
            return null;
        }

        public static object Get(Type type)
        {
            if (services.ContainsKey(type))
            {
                return services[type];
            }
            DebLogger.Error($"There is no {type} in the service hub");
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2">Ранее зарегистрированный тип</typeparam>
        public static void AddMapping<T1, T2>() 
            where T1 : class, IService 
            where T2 : class, IService
        {
            Type t1 = typeof(T1);
            Type t2 = typeof(T2);
            typeMapping.AddOrUpdate(t1, t2, (e1, e2) => t2);
        }

        public static void Dispose()
        {
            foreach (var kvp in services)
            {
                if (kvp.Value != null && kvp.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            services.Clear();
        }
    }
}
