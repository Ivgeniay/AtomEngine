using System.Numerics;

namespace AtomEngine
{
    public interface IRenderSystem : ICommonSystem
    { 
        public void Render(double deltaTime, object? context);
        public void Resize(Vector2 size);
    }
}
