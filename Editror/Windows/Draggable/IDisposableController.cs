using System.Threading.Tasks;

namespace Editor
{
    public interface IDisposableController
    {
        Task PrepareForCloseAsync();
    }
}