using System.Collections.Concurrent;
using System.Threading.Tasks;
using AtomEngine;
using System;
using System.Collections.Generic;

namespace Editor
{

    internal static class ServiceHub
    {
        private static ConcurrentDictionary<Type, IService> services = new ConcurrentDictionary<Type, IService>();
        private static Queue<IService> _queueInitializingService = new Queue<IService>();

        public static void RegisterService<T>() where T : class, IService, new()
        {
            T service = new T();
            services[typeof(T)] = service;
            _queueInitializingService.Enqueue(service);
        }

        public static async Task Initialize(
            Action<Type> OnStartInitializeCollback = null,
            Action<Type> OnInitializedCallback =null
            )
        {
            while (_queueInitializingService.Count > 0) 
            {
                var service = _queueInitializingService.Dequeue();
                if (service != null)
                {
                    OnStartInitializeCollback?.Invoke(service.GetType());
                    await service.Initialize();
                    OnInitializedCallback?.Invoke(service.GetType());
                }
            }
        }

        public static T Get<T>() where T : class, IService
        {
            Type type = typeof(T);
            if (services.ContainsKey(type))
            {
                return (T)services[type];
            }
            else
            {
                DebLogger.Error($"There is no {type} in the service hub");
                return null;
            }
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
