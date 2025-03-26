namespace EngineLib
{
    public class RuntimeResourceManager : IService, IDisposable
    {
        protected MetadataManager _metadataManager;

        protected Dictionary<(string, object), object> _resourceCache = new Dictionary<(string, object), object>();
        protected Dictionary<object, string> _objectToGuidCache = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

        public virtual Task InitializeAsync()
        {
            _metadataManager = ServiceHub.Get<MetadataManager>();
            return Task.CompletedTask;
        }

        public virtual T GetResource<T>(string guid, object context = null) where T : class
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            return GetResource(guid, context) as T;
        }

        public virtual object GetResource(string guid, object context = null)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            if (_resourceCache.TryGetValue((guid, context), out var cachedResource))
                return cachedResource;

            var resource = LoadResourceByGuid(guid, context);

            if (resource != null)
            {
                _resourceCache[(guid, context)] = resource;
                _objectToGuidCache[resource] = guid;
            }

            return resource;
        }

        protected virtual object LoadResourceByGuid(string guid, object context = null)
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose() { }

    }
}
