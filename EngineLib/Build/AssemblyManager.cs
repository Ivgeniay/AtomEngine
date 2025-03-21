using EngineLib;
using System.Reflection;

namespace AtomEngine
{
    public class AssemblyManager : IService
    {
        public static AssemblyManager Instance { get; private set; }
        protected readonly HashSet<Assembly> _assemblies = new();

        public AssemblyManager() { 
            Instance = this;
        }


        public void InitializeAddDomainAssemblies()
        {
            IEnumerable<Assembly> initialAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in initialAssemblies)
                _assemblies.Add(assembly);
        }

        public virtual void ScanDirectory(string path)
        {

            foreach (var file in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var result = LoadAssembly(file);
                    if (!result)
                    {
                        DebLogger.Error($"Не удалось загрузить сборку {file}");
                    }
                }
                catch (AssemblyError ex)
                { 
                }
            }
        }

        private bool LoadAssembly(string path)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(path);

                if (_assemblies.Any(e => e.FullName == assemblyName.FullName))
                {
                    return false;
                }
                var assembly = Assembly.LoadFrom(path);
                _assemblies.Add(assembly);
                return true;
            }
            catch (AssemblyError ex)
            {
                return false;
            }
        }

        public virtual Type? FindType(string typeName, bool isFullName = false)
        {
            foreach (var assembly in _assemblies)
            {
                try
                {
                    Type type = null;
                    if (isFullName) type = assembly.GetTypes().FirstOrDefault(t => t.FullName == typeName);
                    else type = assembly.GetTypes().FirstOrDefault((t => t.Name == typeName));

                    if (type != null)
                        return type;
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
                catch (Exception ex) when (ex is AssemblyError || ex is TypeLoadException)
                {
                    continue;
                }
                catch (AssemblyError ex)
                {
                }
            }
            return null;
        }

        public virtual IEnumerable<Type> FindTypesByInterface<T>(bool isAssignableFrom = true)
        {
            foreach (var assembly in _assemblies)
            {
                foreach (var type in FindTypesInAssembly<T>(assembly, isAssignableFrom))
                {
                    yield return type;
                }
            }
        }

        public virtual void AddAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }

        protected IEnumerable<Type> FindTypesInAssembly<T>(Assembly assembly, bool isAssignableFrom)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (isAssignableFrom)
                {
                    if (typeof(T).IsAssignableFrom(type))
                    {
                        yield return type;
                    }
                }
                else
                {
                    var interfaces = type.GetInterfaces();
                    if (interfaces.Contains(typeof(T)))
                    {
                        yield return type;
                    }
                }
            }
        }

        public virtual Task InitializeAsync() => Task.CompletedTask;
    }
}
