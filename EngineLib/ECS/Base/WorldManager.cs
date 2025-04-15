

namespace AtomEngine
{
    public class WorldManager
    {
        public List<IWorld> worlds = new List<IWorld>();
        private IWorld currentWorld;
        public IWorld CurrentWorld { get 
            {
                if (currentWorld == null) currentWorld = worlds[0];
                return currentWorld;
            }
            set
            {
                currentWorld = value;
            }
        }

        public void AddWorld(IWorld world)
        {
            if (!worlds.Contains(world))
            {
                worlds.Add(world);
            }
        }

        public void RemoveWorld(IWorld world)
        {
            if (worlds.Contains(world))
            {
                worlds.Remove(world);
            }
        }

        public IEnumerable<IWorld> GetWorlds()
        {
            foreach (IWorld world in worlds)
                yield return world;
        }

        public int Count() => worlds.Count;

        public void UpdateSingeThread(double deltaTime)
        {
            CurrentWorld.UpdateSingeThread(deltaTime);
        }

        public void Update(double deltaTime)
        {
            CurrentWorld.Update(deltaTime);
        }

        public void Render(double deltaTime, object? context)
        {
            CurrentWorld.Render(deltaTime, context);
        }

        public void FixedUpdate()
        {
            CurrentWorld.FixedUpdate();
        }
    }
}
