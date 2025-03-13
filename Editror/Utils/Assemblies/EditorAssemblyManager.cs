using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.IO;
using System;
using AtomEngine;

namespace Editor
{
    public class EditorAssemblyManager : AssemblyManager, IService
    {
        public Action? OnUserScriptAsseblyRebuild;

        private Dictionary<TAssembly, Assembly> _assemblyDict = new Dictionary<TAssembly, Assembly>();
        private Dictionary<TAssembly, string> _assemblyMap = new Dictionary<TAssembly, string>
                {
                    { TAssembly.Core, "EngineLib"},
                    { TAssembly.Render, "OpenglLib" },
                    { TAssembly.SilkOpenGL, "Silk.NET.OpenGL" },
                    { TAssembly.SilkMath, "Silk.NET.Maths" },
                    { TAssembly.ComponentGenerator, "ComponentGenerator" },
                    { TAssembly.NewtonsoftJson, "Newtonsoft.Json" },
                    { TAssembly.SilkNetCore, "Silk.NET.Core"},
                    { TAssembly.CommonLib, "CommonLib"}
                };

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

        public override Type? FindType(string typeName)
        {
            var tp = _user_script_assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (tp != null) return tp;

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

        public Assembly GetAssembly(TAssembly assembly) => _assemblyDict[assembly];

        public override IEnumerable<Type> FindTypesByInterface<T>(bool isAssignableFrom = true)
        {
            IEnumerable<Type> ts;

            foreach (var type in FindTypesInAssembly<T>(_user_script_assembly, isAssignableFrom))
            {
                yield return type;
            }

            foreach (var assembly in _assemblies)
            {
                if (IsSystemAssembly(assembly))
                    continue;

                foreach (var type in FindTypesInAssembly<T>(assembly, isAssignableFrom))
                {
                    yield return type;
                }
            }
        }

        private bool IsSystemAssembly(Assembly assembly)
        {
            string assemblyName = assembly.FullName;
            return assemblyName.StartsWith("System") ||
                   assemblyName.StartsWith("Avalonia") ||
                   assemblyName.StartsWith("Microsoft") ||
                   assemblyName.Contains("Generator");
        }

        public override void AddAssembly(Assembly assembly)
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
        SilkNetCore,
        CommonLib,
        ComponentGenerator,
        NewtonsoftJson,
    }
}
