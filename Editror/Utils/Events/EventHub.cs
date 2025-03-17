using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Reflection;
using AtomEngine;
using System;

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
                foreach (var subscriber in list)
                {
                    try
                    {
                        ((Action<T>)subscriber)(evt);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            if (ex.Message == "Call from invalid thread")
                            {
                                Dispatcher.UIThread.Invoke(new Action(() =>
                                {
                                    ((Action<T>)subscriber)(evt);
                                }));
                            }
                        }
                        catch
                        {
                            DebLogger.Error($"(EventHub) Event error: {type.Name} message: {ex.Message}");
                        }
                    }
                    catch
                    {
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
