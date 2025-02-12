namespace AtomEngine
{
    public interface ISystem
    {
        IWorld World { get; }
        public void Update(double deltaTime);
    }
}
