namespace AtomEngine
{
    public interface ICommonSystem
    {
        IWorld World { get; set; }
        public void Initialize();
    }
}
