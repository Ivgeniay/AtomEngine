using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using AtomEngine;
using System.IO;
using EngineLib;
using System;

namespace Editor
{
    public class EditorAssemblyManager : AssemblyManager, IService
    {
        private Dictionary<Type, string> _assemblyMap = new Dictionary<Type, string>
                {
                    { typeof(CoreAssembly), "EngineLib"},
                    { typeof(OpenGlLibAssembly), "OpenglLib" },
                    { typeof(SilkNetOpenGlAssembly), "Silk.NET.OpenGL" },
                    { typeof(SilkNetMathAssembly), "Silk.NET.Maths" },
                    { typeof(ComponentGenAssembly), "ComponentGenerator" },
                    { typeof(NewtonJsonAssembly), "Newtonsoft.Json" },
                    { typeof(SilkNetCoreAssembly), "Silk.NET.Core"},
                    { typeof(CommonAssembly), "CommonLib"}
                };

        private bool _isInitialized = false;

        public override Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() => {
                IEnumerable<Assembly> initialAssemblies = AppDomain.CurrentDomain.GetAssemblies();



                foreach (var assembly in initialAssemblies)
                    _assemblies.Add(assembly);

                string baseDirectry = ServiceHub
                                        .Get<EditorDirectoryExplorer>()
                                        .GetPath<BaseDirectory>();

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
            var pluginPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<PluginsDirectory>();
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

        public Type? FindTypeInUserAssembly(string typeName, bool isFullName = false)
        {
            Type type = null;
            if (isFullName) type = _assemblyDict[typeof(UserScriptAssembly)].GetTypes().FirstOrDefault(t => t.FullName == typeName);
            else type = _assemblyDict[typeof(UserScriptAssembly)].GetTypes().FirstOrDefault(t => t.Name == typeName);

            return type;
        }

        public override Type? FindType(string typeName, bool isFullName = false)
        {
            Type type = FindTypeInUserAssembly(typeName, isFullName);
            if (type != null) return type;

            foreach (var assembly in _assemblies)
            {
                try
                {
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
        public override IEnumerable<Type> FindTypesByInterface<T>(bool isAssignableFrom = true)
        {
            IEnumerable<Type> ts;

            foreach (var type in FindTypesInAssembly<T>(_assemblyDict[typeof(UserScriptAssembly)], isAssignableFrom))
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
            _assemblyDict[typeof(UserScriptAssembly)] = assembly;
        }
        internal Assembly GetUserScriptAssembly()
        {
            if (_assemblyDict[typeof(UserScriptAssembly)] == null)
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
                    return _assemblyDict[typeof(UserScriptAssembly)];
                }
                catch
                {
                    throw;
                }
            }
            return _assemblyDict[typeof(UserScriptAssembly)];
        }

        internal void FreeCache()
        {
            _assemblyDict[typeof(UserScriptAssembly)] = null;
        }

        internal IEnumerable<Type> GetTypesByAttribute<T>() where T : Attribute
        {
            foreach (var assembly in _assemblyDict)
            {
                if (assembly.Key == typeof(UserScriptAssembly)) continue;

                var types = assembly.Value.GetTypes();
                foreach(Type t in types)
                {
                    if (t.GetCustomAttribute<T>(true) != null)
                        yield return t;
                }
            }
        }
    }
}
