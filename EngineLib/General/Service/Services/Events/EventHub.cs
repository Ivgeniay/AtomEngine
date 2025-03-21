using AtomEngine;

namespace EngineLib
{
    public class EventHub : IService
    {
        private Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        private Dictionary<Type, List<Action<Exception, Delegate, EventHubEvent>>> _errorHandlers = new Dictionary<Type, List<Action<Exception, Delegate, EventHubEvent>>>();

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
                            ErrorHandler(ex, subscriber, evt);
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

        private void ErrorHandler<T>(Exception ex, Delegate subscriber, T evt) where T : EventHubEvent
        {
            var exceptionType = ex.GetType();

            if (_errorHandlers.TryGetValue(exceptionType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler(ex, subscriber, evt);
                    }
                    catch (Exception handlerEx)
                    {
                        DebLogger.Error($"(EventHub) Error in error handler: {handlerEx.Message}");
                    }
                }
            }
        }

        public void RegisterErrorHandler<TException>(Action<Exception, Delegate, EventHubEvent> handler)
        where TException : Exception
        {
            var exceptionType = typeof(TException);

            if (!_errorHandlers.TryGetValue(exceptionType, out var handlers))
            {
                handlers = new List<Action<Exception, Delegate, EventHubEvent>>();
                _errorHandlers[exceptionType] = handlers;
            }

            handlers.Add(handler);
        }

        public Task InitializeAsync() => Task.CompletedTask;
    }

    public class EventHubEvent
    {

    }


    //internal class EventHub : IService
    //{
    //    private Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    //    public void Subscribe<T>(Action<T> action) where T : EventHubEvent
    //    {
    //        if (action == null) throw new NullValueError("Attempt to register null Action to EventHub");

    //        var type = typeof(T);
    //        if (!_subscribers.TryGetValue(type, out var list))
    //        {
    //            list = new List<Delegate>();
    //            _subscribers[type] = list;
    //        }
    //        list.Add(action);
    //    }

    //    public void SendEvent<T>(T evt) where T : EventHubEvent
    //    {
    //        var type = typeof(T);
    //        if (_subscribers.TryGetValue(type, out var list))
    //        {
    //            foreach (var subscriber in list)
    //            {
    //                try
    //                {
    //                    ((Action<T>)subscriber)(evt);
    //                }
    //                catch (Exception ex)
    //                {
    //                    try
    //                    {
    //                        if (ex.Message == "Call from invalid thread")
    //                        {
    //                            Dispatcher.UIThread.Invoke(new Action(() =>
    //                            {
    //                                ((Action<T>)subscriber)(evt);
    //                            }));
    //                        }
    //                    }
    //                    catch
    //                    {
    //                        DebLogger.Error($"(EventHub) Event error: {type.Name} message: {ex.Message}");
    //                    }
    //                }
    //                catch
    //                {
    //                }
    //            }
    //        }
    //    }

    //    public Task InitializeAsync() => Task.CompletedTask;
    //}
}
