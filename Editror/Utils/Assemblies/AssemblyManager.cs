using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.IO;
using System;

namespace Editor
{
    public class AssemblyManager : IService
    {
        public Action OnUserScriptAsseblyRebuild;

        private Dictionary<TAssembly, Assembly> _assemblyDict = new Dictionary<TAssembly, Assembly>();
        private Dictionary<TAssembly, string> _assemblyMap = new Dictionary<TAssembly, string>
                {
                    { TAssembly.Core, "EngineLib"},
                    { TAssembly.Render, "OpenglLib" },
                    { TAssembly.SilkOpenGL, "Silk.NET.OpenGL" },
                    { TAssembly.SilkMath, "Silk.NET.Maths" },
                    { TAssembly.ComponentGenerator, "ComponentGenerator" },
                    { TAssembly.NewtonsoftJson, "Newtonsoft.Json" },
                };

        private readonly HashSet<Assembly> _assemblies = new();
        private Assembly _user_script_assembly;
        private bool _isInitialized = false;

        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() => {
                IEnumerable<Assembly> initialAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in initialAssemblies)
                    _assemblies.Add(assembly);

                string baseDirectry = ServiceHub
                                        .Get<DirectoryExplorer>()
                                        .GetPath(DirectoryType.Base);

                foreach (var filePath in Directory.GetFiles(baseDirectry, "*.dll"))
                {
                    string fileName = Path.GetFileName(filePath);

                    foreach (var pair in _assemblyMap)
                    {
                        if (fileName.Equals(pair.Value + ".dll"))
                        {
                            Assembly assembly = Assembly.LoadFrom(filePath);
                            if (assembly != null)
                            {
                                _assemblyDict[pair.Key] = assembly;
                                _assemblies.Add(assembly);
                            }
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
            var pluginPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.Plugins);
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
                if (assembly.FullName.StartsWith("System") || 
                    assembly.FullName.StartsWith("Avalonia") ||
                    assembly.FullName.StartsWith("Microsoft") ||
                    assembly.FullName.Contains("Generator")
                    )
                    continue;

                var types = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(T).IsAssignableFrom(t));

                foreach (var type in types)
                    yield return type;
            }
        }

        internal void AddAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }

        internal void UpdateScriptAssembly(Assembly assembly)
        {
            _user_script_assembly = assembly;
            OnUserScriptAsseblyRebuild?.Invoke();
        }
        internal Assembly GetUserScriptAssembly()
        {
            if (_user_script_assembly == null)
            {
                try
                {
                    ProjectConfigurations pConf = ServiceHub
                        .Get<Configuration>()
                        .GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);

                    UpdateScriptAssembly(
                        ServiceHub
                            .Get<ScriptProjectGenerator>()
                            .LoadCompiledAssembly(pConf.BuildType)
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
        SilkOpenGL,
        ComponentGenerator,
        NewtonsoftJson,
    }
}
