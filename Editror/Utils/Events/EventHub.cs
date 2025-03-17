using AtomEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Avalonia.Threading;

namespace Editor
{
    internal class EventHub : IService
    {
        private Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> action) where T : EventHubEvent
        {
            if (action == null) throw new NullValueError("Attempt to register null Action to EventHub");

            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(action);
        }

        public void SendEvent<T>(T evt) where T : EventHubEvent
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
                        if (ex.Message == "Call from invalid thread")
                        {
                            DebLogger.Error($"Попытка перенаправить действие в поток UI");
                            Dispatcher.UIThread.Invoke(new Action(() =>
                            {
                                ((Action<T>)subscriber)(evt);
                            }));
                        }
                    }
                    catch
                    {
                        DebLogger.Error($"Ошибка при отправке события {type.Name}");
                    }
                }
            }
        }

        public Task InitializeAsync() => Task.CompletedTask;
    }

    internal class EventHubEvent
    {

    }

    internal class AssemblyUnloadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
    internal class AssemblyUploadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
