

namespace EngineLib
{
    public interface ISystem
    {
        IWorld World { get; }
        public abstract void Update(float deltaTime);
    }
}
