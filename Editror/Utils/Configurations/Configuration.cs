using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using EngineLib;
using System.IO;

namespace Editor
{
    public class Configuration : IService
    {
        private Dictionary<ConfigurationSource, string> configsSource = new Dictionary<ConfigurationSource, string>();
        private Dictionary<ConfigurationSource, string> configsCache = new Dictionary<ConfigurationSource, string>();

        private const string EXPLORER_CONFIG_FILE = "explorer.config";
        private const string SCENE_CONFIG_FILE = "scenes.config";
        private const string PROJECT_CONFIG_FILE = "project.config";
        private const string WINDOW_MANAGER_CONFIG_FILE = "w_manager.config";

        private static bool _isInitialized = false;
        public Configuration() { }

        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() =>
            {
                configsSource.Add(
                    ConfigurationSource.ExplorerConfigs,
                    Path.Combine(
                        ServiceHub.Get<EditorDirectoryExplorer>().GetPath<ConfigurationsDirectory>(),
                        EXPLORER_CONFIG_FILE));
                configsSource.Add(
                    ConfigurationSource.SceneConfigs,
                    Path.Combine(
                        ServiceHub.Get<EditorDirectoryExplorer>().GetPath<ConfigurationsDirectory>(),
                        SCENE_CONFIG_FILE));
                configsSource.Add(
                    ConfigurationSource.ProjectConfigs,
                    Path.Combine(
                        ServiceHub.Get<EditorDirectoryExplorer>().GetPath<ConfigurationsDirectory>(),
                        PROJECT_CONFIG_FILE));
                configsSource.Add(
                    ConfigurationSource.WindowManagerConfigs,
                    Path.Combine(
                        ServiceHub.Get<EditorDirectoryExplorer>().GetPath<ConfigurationsDirectory>(),
                        WINDOW_MANAGER_CONFIG_FILE));

                foreach (KeyValuePair<ConfigurationSource, string> kvp in configsSource)
                {
                    var key = kvp.Key;
                    string value = string.Empty;

                    if (!File.Exists(kvp.Value))
                    {
                        switch (kvp.Key)
                        {
                            case ConfigurationSource.ProjectConfigs:
                                value = JsonConvert.SerializeObject(new ProjectConfigurations(), GlobalDeserializationSettings.Settings);
                                break;
                            case ConfigurationSource.SceneConfigs:
                                value = JsonConvert.SerializeObject(new SceneConfiguration(), GlobalDeserializationSettings.Settings);
                                break;
                            case ConfigurationSource.ExplorerConfigs:
                                value = JsonConvert.SerializeObject(new ExplorerConfigurations(), GlobalDeserializationSettings.Settings);
                                break;
                            case ConfigurationSource.WindowManagerConfigs:
                                value = JsonConvert.SerializeObject(new WindowManagerConfiguration(), GlobalDeserializationSettings.Settings);
                                break;

                        }

                        using (FileStream file = File.Create(kvp.Value))
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(value);
                            file.Write(bytes, 0, bytes.Length);
                        }
                    }
                    else
                    {
                        value = File.ReadAllText(kvp.Value);
                    }
                    configsCache.Add(key, value);
                }

                _isInitialized = true;
            });
        }

        public async void SafeConfiguration(ConfigurationSource source, object s)
        {
            var ser = JsonConvert.SerializeObject(s, Formatting.Indented);
            configsCache[source] = ser;
            await File.WriteAllTextAsync(configsSource[source], ser);
        }

        public string GetConfiguration(ConfigurationSource source) => configsCache[source];
        public T GetConfiguration<T>(ConfigurationSource source)
        {
            T des = JsonConvert.DeserializeObject<T>(configsCache[source], GlobalDeserializationSettings.Settings);
            return des;
        }
    }

    public class ProjectConfigurations
    {
        public string ProjectName { get; set; } = "NewProject";
        public string AssemblyName { get; set; } = "CSharp_Assembly";
        public BuildType BuildType { get; set; } = BuildType.Debug;

        public string RootNamespace { get; set; } = "NewProject";
    }
    public class ExplorerConfigurations
    {
        public List<string> ExcludeExtension { get; set; }

        public ExplorerConfigurations()
        {
            ExcludeExtension = new List<string>() { ".exe", EditorMetadataManager.META_EXTENSION };
        }
    }

    public class SceneConfiguration
    {
        
    }
}
