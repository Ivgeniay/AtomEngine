

namespace AtomEngine
{
    public interface ISystem
    {
        IWorld World { get; }
        public abstract void Update(double deltaTime);
    }
}
