using System.Collections.Concurrent;
using System.Text;

namespace AtomEngine
{
    public class SystemDependencyGraph
    {
        private readonly ConcurrentDictionary<ISystem, HashSet<ISystem>> _dependencies = new();
        private readonly ConcurrentDictionary<ISystem, HashSet<ISystem>> _dependents = new();
        private volatile List<List<ISystem>>? _cachedLevels;
        private readonly ReaderWriterLockSlim _cacheLock = new();

        public event Action? GraphChanged;

        public void AddSystem(ISystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            var added = _dependencies.TryAdd(system, new HashSet<ISystem>()) |
                       _dependents.TryAdd(system, new HashSet<ISystem>());

            if (added)
            {
                InvalidateCache();
                GraphChanged?.Invoke();
            }
        }

        public void RemoveSystem(ISystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (!_dependencies.ContainsKey(system))
                return;

            HashSet<ISystem> dependencies;
            HashSet<ISystem> dependents;

            lock (_dependencies[system])
            {
                dependencies = new HashSet<ISystem>(_dependencies[system]);
            }

            lock (_dependents[system])
            {
                dependents = new HashSet<ISystem>(_dependents[system]);
            }
            foreach (var dependent in dependents)
            {
                foreach (var dependency in dependencies)
                {
                    if (dependent != dependency)
                    {
                        try
                        {
                            AddDependency(dependent, dependency);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("cycle"))
                        {
                            DebLogger.Debug($"Warning: Skipping transitive dependency {dependent.GetType().Name} -> {dependency.GetType().Name} which would create a cycle");
                        }
                    }
                }
            }

            foreach (var dependent in dependents)
            {
                lock (_dependencies[dependent])
                {
                    _dependencies[dependent].Remove(system);
                }
            }

            foreach (var dependency in dependencies)
            {
                lock (_dependents[dependency])
                {
                    _dependents[dependency].Remove(system);
                }
            }

            _dependencies.TryRemove(system, out _);
            _dependents.TryRemove(system, out _);

            InvalidateCache();
            GraphChanged?.Invoke();
        }

        public void AddDependency(ISystem dependent, ISystem dependency)                                      
        {
            if (dependent == null)
                throw new ArgumentNullException(nameof(dependent));
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));
            if (dependent == dependency)
                throw new ArgumentException("System cannot depend on itself");

            AddSystem(dependent);
            AddSystem(dependency);

            // Используем lock для безопасного обновления множеств
            lock (_dependencies[dependent])
                lock (_dependents[dependency])
                {
                    if (_dependencies[dependent].Add(dependency))
                    {
                        _dependents[dependency].Add(dependent);

                        // Проверяем циклические зависимости после добавления
                        try
                        {
                            if (HasCircularDependency())
                            {
                                // Откатываем изменения при обнаружении цикла
                                _dependencies[dependent].Remove(dependency);
                                _dependents[dependency].Remove(dependent);
                                throw new InvalidOperationException("Adding this dependency would create a cycle");
                            }
                        }
                        catch
                        {
                            // Откатываем изменения при любой ошибке
                            _dependencies[dependent].Remove(dependency);
                            _dependents[dependency].Remove(dependent);
                            throw;
                        }

                        InvalidateCache();
                        GraphChanged?.Invoke();
                    }
                }
        }

        public List<List<ISystem>> GetExecutionLevels()
        {
            // Пробуем получить кэшированное значение
            var cached = _cachedLevels;
            if (cached != null)
                return cached;

            // Если кэша нет, вычисляем с блокировкой
            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                cached = _cachedLevels;
                if (cached != null)
                    return cached;

                _cacheLock.EnterWriteLock();
                try
                {
                    var levels = CalculateExecutionLevels();
                    _cachedLevels = levels;
                    return levels;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
        }

        private void InvalidateCache()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cachedLevels = null;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private List<List<ISystem>> CalculateExecutionLevels()
        {
            // Создаем копии для безопасной работы
            Dictionary<ISystem, HashSet<ISystem>> dependenciesCopy;
            HashSet<ISystem> remainingSystems;

            lock (_dependencies)
            {
                dependenciesCopy = _dependencies.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<ISystem>(kvp.Value)
                );
                remainingSystems = new HashSet<ISystem>(_dependencies.Keys);
            }

            var result = new List<List<ISystem>>();

            while (remainingSystems.Any())
            {
                var currentLevel = remainingSystems
                    .Where(system => !dependenciesCopy[system].Any())
                    .ToList();

                if (!currentLevel.Any())
                    throw new InvalidOperationException("Circular dependency detected during execution level calculation");

                result.Add(currentLevel);

                foreach (var system in currentLevel)
                {
                    remainingSystems.Remove(system);
                    foreach (var dependent in _dependents[system])
                    {
                        if (dependenciesCopy.TryGetValue(dependent, out var dependencies))
                        {
                            dependencies.Remove(system);
                        }
                    }
                }
            }

            return result;
        }

        public bool HasDependency(ISystem dependent, ISystem dependency)
        {
            if (_dependencies.TryGetValue(dependent, out var deps))
            {
                lock (deps)
                {
                    return deps.Contains(dependency);
                }
            }
            return false;
        }

        public IReadOnlySet<ISystem> GetDependencies(ISystem system)
        {
            if (_dependencies.TryGetValue(system, out var deps))
            {
                lock (deps)
                {
                    return new HashSet<ISystem>(deps);
                }
            }
            return new HashSet<ISystem>();
        }

        private bool HasCircularDependency()
        {
            var state = new CycleDetectionState();

            // Создаем защищенную копию зависимостей для проверки
            Dictionary<ISystem, HashSet<ISystem>> dependenciesCopy;
            lock (_dependencies)
            {
                dependenciesCopy = _dependencies.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<ISystem>(kvp.Value)
                );
            }

            foreach (var system in dependenciesCopy.Keys)
            {
                if (HasCircularDependencyDFS(system, state, dependenciesCopy))
                {
                    // Сохраняем информацию о найденном цикле для диагностики
                    var cyclePath = string.Join(" -> ", state.CyclePath);
                    throw new InvalidOperationException(
                        $"Circular dependency detected. Cycle path: {cyclePath}");
                }
            }

            return false;
        }

        private bool HasCircularDependencyDFS(
            ISystem current,
            CycleDetectionState state,
            Dictionary<ISystem, HashSet<ISystem>> dependencies)
        {
            // Если система уже в стеке рекурсии, мы нашли цикл
            if (state.RecursionStack.Contains(current))
            {
                // Добавляем текущую систему, чтобы замкнуть цикл в пути
                state.CyclePath.Add(current);
                return true;
            }

            // Если мы уже посещали эту систему и не нашли цикл, пропускаем
            if (state.Visited.Contains(current))
                return false;

            // Добавляем систему в оба множества для отслеживания
            state.Visited.Add(current);
            state.RecursionStack.Add(current);
            state.CyclePath.Add(current);

            // Проверяем все зависимости текущей системы
            if (dependencies.TryGetValue(current, out var currentDependencies))
            {
                foreach (var dependency in currentDependencies)
                {
                    if (HasCircularDependencyDFS(dependency, state, dependencies))
                    {
                        return true;
                    }
                }
            }

            // Убираем систему из стека рекурсии и пути, так как мы закончили её проверку
            state.RecursionStack.Remove(current);
            state.CyclePath.RemoveAt(state.CyclePath.Count - 1);
            return false;
        }

        public string GetDependencyGraphInfo()
        {
            var result = new StringBuilder();
            foreach (var (system, deps) in _dependencies)
            {
                result.AppendLine($"System: {system.GetType().Name}");
                foreach (var dep in deps)
                {
                    result.AppendLine($"  -> {dep.GetType().Name}");
                }
            }
            return result.ToString();
        }

        private class CycleDetectionState
        {
            public HashSet<ISystem> Visited { get; } = new();
            public HashSet<ISystem> RecursionStack { get; } = new();
            public List<ISystem> CyclePath { get; } = new();
        }
    }
}
