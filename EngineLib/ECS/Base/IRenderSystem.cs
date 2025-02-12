using System.Numerics;

namespace AtomEngine
{
    public interface IRenderSystem
    {
        IWorld World { get; }
        public void Render(double deltaTime);
        public void Resize(Vector2 size);
    }
}
