namespace EngineLib
{
    public class ExcludeSerializationTypeService : IService
    {
        protected readonly HashSet<Type> _excludeTypes = new HashSet<Type>();
        public virtual Task InitializeAsync() => Task.CompletedTask;

        public void AddExcludeType(Type excludeType)
        {
            _excludeTypes.Add(excludeType);
        }

        public void RemoveExcludeType(Type excludeType)
        {
            if (_excludeTypes.Contains(excludeType))
            {
                _excludeTypes.Remove(excludeType);
            }
        }

        public bool IsExcludedType(Type type) => _excludeTypes.Any(dt => dt.IsAssignableFrom(type));
    }
}
