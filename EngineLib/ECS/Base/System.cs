namespace EngineLib
{
    public abstract class System
    {
        protected World World { get; }
        protected System(World world)
        {
            World = world;
        }

        public abstract void Update(float deltaTime);
    }
}
