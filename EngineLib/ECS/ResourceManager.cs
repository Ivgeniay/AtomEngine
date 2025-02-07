namespace AtomEngine
{
    public class ResourceManager
    {
        private readonly Dictionary<Entity, HashSet<IDisposable>> _resources = new();

        public void RegisterResource(Entity owner, IDisposable resource)
        {
            if (!_resources.TryGetValue(owner, out var resources))
            {
                resources = new HashSet<IDisposable>();
                _resources[owner] = resources;
            }
            resources.Add(resource);
        }

        public void CleanupEntity(ref Entity entity)
        {
            if (_resources.TryGetValue(entity, out var resources))
            {
                foreach (var resource in resources)
                {
                    resource.Dispose();
                }
                _resources.Remove(entity);
            }
        }

        public void CleanupResource(Entity entity, IDisposable resource)
        {
            if (_resources.TryGetValue(entity, out var resources))
            {
                if (resources.Remove(resource))
                {
                    resource.Dispose();
                }

                if (resources.Count == 0)
                {
                    _resources.Remove(entity);
                }
            }
        }
    }
}
