using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;

namespace Editor
{
    public class AssemblyManager : IService
    {
        private string _coreDllName = "EngineLib";
        private string _renderDllName = "OpenglLib";
        private Dictionary<TAssembly, string> _assemblyMap;
        private Dictionary<TAssembly, Assembly> _assemblyDict = new Dictionary<TAssembly, Assembly>();
        public static AssemblyManager Instance { get; } = new();

        private readonly HashSet<Assembly> _assemblies = new();
        private Assembly _user_script_assembly;
        private bool _isInitialized = false;

        public Task Initialize()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() => {
                IEnumerable<Assembly> initialAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            _assemblyMap = new Dictionary<TAssembly, string>
            {
                { TAssembly.Core, "EngineLib"},
                { TAssembly.Render, "OpenglLib" },
                { TAssembly.SilkOpenGL, "Silk.NET.OpenGL" },
                { TAssembly.SilkMath, "Silk.NET.Maths" }
            };

                foreach (var assembly in initialAssemblies)
                {
                    _assemblies.Add(assembly);
                }

                var baseDirectry = DirectoryExplorer.GetPath(DirectoryType.Base);
                foreach (var filePath in Directory.GetFiles(baseDirectry, "*.dll"))
                {
                    string fileName = Path.GetFileName(filePath);

                    foreach (var pair in _assemblyMap)
                    {
                        if (fileName.Equals(pair.Value + ".dll"))
                        {
                            Assembly assembly = Assembly.LoadFrom(filePath);
                            _assemblyDict[pair.Key] = assembly;
                            _assemblies.Add(assembly);
                            break;
                        }
                    }
                }

                ScanPluginsDirectory();

                _isInitialized = true;
            });
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

        public Assembly GetAssembly(TAssembly assembly) => _assemblyDict[assembly];

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

        internal void UpdateScriptAssembly(Assembly assembly) => _user_script_assembly = assembly;
        internal Assembly GetUserScriptAssembly()
        {
            if (_user_script_assembly == null)
            {
                try
                {
                    ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                    UpdateScriptAssembly(
                        ServiceHub.Get<ScriptProjectGenerator>().LoadCompiledAssembly(pConf.BuildType)
                        );
                    return _user_script_assembly;
                }
                catch
                {
                    throw;
                }
            }
            return _user_script_assembly;
        }
    }

    public enum TAssembly
    {
        Core,
        Render,
        SilkMath,
        SilkOpenGL
    }
}
