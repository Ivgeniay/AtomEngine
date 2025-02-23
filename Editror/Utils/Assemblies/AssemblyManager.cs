using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.IO;
using System;

namespace Editor
{
    public class AssemblyManager
    {
        public static AssemblyManager Instance { get; } = new();

        private readonly HashSet<Assembly> _assemblies = new();
        private readonly string _pluginsPath;

        private AssemblyManager()
        {
            _pluginsPath = Path.Combine(AppContext.BaseDirectory, "Plugins");
        }

        public void Initialize(params Assembly[] initialAssemblies)
        {
            foreach (var assembly in initialAssemblies)
            {
                _assemblies.Add(assembly);
            }

            if (Directory.Exists(_pluginsPath))
            {
                ScanPluginsDirectory();
            }
            else
            {
                Directory.CreateDirectory(_pluginsPath);
            }
        }

        private void ScanPluginsDirectory()
        {
            foreach (var file in Directory.GetFiles(_pluginsPath, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    _assemblies.Add(assembly);
                }
                catch (FileNotFoundError ex)
                { }
            }
        }

        public Type? FindType(string typeName)
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
                catch (FileNotFoundError ex)
                { }
            }
            return null;
        }

        public IEnumerable<Type> FindTypesByInterface<T>()
        {
            foreach (var assembly in _assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(T).IsAssignableFrom(t));

                foreach (var type in types)
                    yield return type;
            }
        }

        public void AddAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }

        public void AddAssemblyFromFile(string path)
        {
            try
            {
                var assembly = Assembly.LoadFrom(path);
                _assemblies.Add(assembly);
            }
            catch (FileNotFoundError ex)
            {
                throw;
            }
        }
    }
}
