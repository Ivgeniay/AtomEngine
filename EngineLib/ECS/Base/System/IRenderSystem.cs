using System.Numerics;

namespace AtomEngine
{
    public interface IRenderSystem : ICommonSystem
    { 
        public void Render(double deltaTime);
        public void Resize(Vector2 size);
    }
}
