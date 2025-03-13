using System.Reflection;

namespace AtomEngine
{
    public class AssemblyManager
    {
        protected readonly HashSet<Assembly> _assemblies = new();

        public virtual void ScanDirectory(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    _assemblies.Add(assembly);
                }
                catch (AssemblyError ex)
                { }
            }
        }

        public virtual Type? FindType(string typeName)
        {
            foreach (var assembly in _assemblies)
            {
                try
                {
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == typeName);

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

    }
}
