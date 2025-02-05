namespace EngineLib
{
    public class SystemDependencyGraph
    {
        private readonly Dictionary<System, HashSet<System>> _dependencies = new();
        private readonly Dictionary<System, HashSet<System>> _dependents = new();
        private readonly HashSet<System> _allSystems = new();

        public void AddSystem(System system)
        {
            _allSystems.Add(system);
            if (!_dependencies.ContainsKey(system))
                _dependencies[system] = new HashSet<System>();
            if (!_dependents.ContainsKey(system))
                _dependents[system] = new HashSet<System>();
        }

        public void AddDependency(System dependent, System dependency)
        {
            if (dependent == null || dependency == null)
                throw new ArgumentNullException();

            AddSystem(dependent);
            AddSystem(dependency);

            _dependencies[dependent].Add(dependency);
            _dependents[dependency].Add(dependent);
        }

        public List<List<System>> GetExecutionLevels()
        {
            var result = new List<List<System>>();
            var remainingSystems = new HashSet<System>(_allSystems);
            var dependenciesCopy = _dependencies.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<System>(kvp.Value)
            );

            while (remainingSystems.Any())
            {
                // Находим системы без зависимостей
                var currentLevel = remainingSystems
                    .Where(system => !dependenciesCopy.ContainsKey(system) ||
                                   !dependenciesCopy[system].Any())
                    .ToList();

                if (!currentLevel.Any())
                    throw new Exception("Circular dependency detected");

                // Добавляем уровень
                result.Add(currentLevel);

                // Удаляем системы текущего уровня из оставшихся
                foreach (var system in currentLevel)
                {
                    remainingSystems.Remove(system);

                    // Удаляем текущую систему из зависимостей других систем
                    foreach (var dependent in _dependents.GetValueOrDefault(system, new HashSet<System>()))
                    {
                        if (dependenciesCopy.ContainsKey(dependent))
                        {
                            dependenciesCopy[dependent].Remove(system);
                        }
                    }
                }
            }

            return result;
        }
    }
}
