using Microsoft.Extensions.DependencyInjection;

namespace AtomEngine.Services
{
    public interface IDepency
    {
        public T GetService<T>();
        //public void BuildContainer();
        //public IServiceCollection GetServiceCollection();
    }
}
