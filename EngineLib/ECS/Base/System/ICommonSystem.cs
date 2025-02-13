using System.Numerics; 

namespace AtomEngine
{
    public interface ICommonSystem
    {
        IWorld World { get; }
        public void Initialize();
    }
}
