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

        public void Initialize(IEnumerable<Assembly> initialAssemblies)
        {
            foreach (var assembly in initialAssemblies)
            {
                _assemblies.Add(assembly);
            }

            var baseDirectry = DirectoryExplorer.GetPath(DirectoryType.Base);
            foreach (var file in Directory.GetFiles(baseDirectry, "*.dll"))
            {
                if (file.IndexOf("EngineLib") != -1)
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

            ScanPluginsDirectory();
        }

        public void ScanPluginsDirectory()
        {
            var pluginPath = DirectoryExplorer.GetPath(DirectoryType.Plugins);
            foreach (var file in Directory.GetFiles(pluginPath, "*.dll"))
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
                catch (AssemblyError ex)
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
