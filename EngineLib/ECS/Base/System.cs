namespace AtomEngine
{
    public abstract class System : ISystem
    {
        private World _world;
        public IWorld World => _world;

        protected System(World world) =>
            _world = world;

        public abstract void Update(double deltaTime);
        public virtual void Initialize() { }
    }
}
