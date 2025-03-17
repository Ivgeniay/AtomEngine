using AtomEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Editor
{
    internal class EventHub : IService
    {
        private Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> action)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(action);
        }

        public void SendEvent<T>(T evt)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                foreach (var subscriber in list.ToArray())
                {
                    try
                    {
                        ((Action<T>)subscriber)(evt);
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка при отправке события {type.Name}: {ex.Message}");
                    }
                }
            }
        }

        public Task InitializeAsync() => Task.CompletedTask;
    }
}
