namespace EngineLib
{
    public abstract class System : ISystem
    {
        private World _world;
        IWorld ISystem.World => _world;

        protected System(World world) =>
            _world = world;

        public abstract void Update(float deltaTime);
    }
}
