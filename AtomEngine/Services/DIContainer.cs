using Microsoft.Extensions.DependencyInjection;

namespace AtomEngine.Services
{
    public class DIContainer : IDisposable
    {
        protected readonly DIContainer? parentContainer;
        protected IServiceCollection serviceCollection;
        protected ServiceProvider? serviceProvider;

        public DIContainer(DIContainer parentContainer = null)
        {
            this.parentContainer = parentContainer;
            serviceCollection = new ServiceCollection();
        }

        public void BuildContainer() => serviceProvider = serviceCollection.BuildServiceProvider();
        public IServiceCollection GetServiceCollection() => serviceCollection;
        public T GetService<T>()
        {
            T result = serviceProvider.GetService<T>();
            if (result == null)
            {
                if (parentContainer != null) return parentContainer.GetService<T>();
                else throw new Exception($"Service of type {typeof(T)} not found");
            }
            else return result;
        }
        public void Dispose() => serviceProvider?.Dispose();
    }

    public class SceneDIContainer : DIContainer
    {
        public SceneDIContainer(DIContainer parentContainer) : base(parentContainer) { }
    }

    public class AtomObjectContainer : DIContainer
    {
        public AtomObjectContainer(DIContainer parentContainer) : base(parentContainer) { }
    }
}
