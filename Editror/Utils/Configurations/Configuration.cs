using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    public class Configuration : IService
    {
        private Dictionary<ConfigurationSource, string> configsSource = new Dictionary<ConfigurationSource, string>();
        private Dictionary<ConfigurationSource, string> configsCache = new Dictionary<ConfigurationSource, string>();

        private const string EXPLORER_CONFIG_FILE = "explorer.config";
        private const string SCENE_CONFIG_FILE = "scenes.config";
        private const string PROJECT_CONFIG_FILE = "project.config";

        private static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        private static bool _isInitialized = false;
        
        public Configuration() { }

        public Task Initialize()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() =>
            {
                configsSource.Add(
                    ConfigurationSource.ExplorerConfigs,
                    Path.Combine(
                        DirectoryExplorer.GetPath(DirectoryType.Configurations),
                        EXPLORER_CONFIG_FILE));
                configsSource.Add(
                    ConfigurationSource.SceneConfigs,
                    Path.Combine(
                        DirectoryExplorer.GetPath(DirectoryType.Configurations),
                        SCENE_CONFIG_FILE));
                configsSource.Add(
                    ConfigurationSource.ProjectConfigs,
                    Path.Combine(
                        DirectoryExplorer.GetPath(DirectoryType.Configurations),
                        PROJECT_CONFIG_FILE));

                foreach (KeyValuePair<ConfigurationSource, string> kvp in configsSource)
                {
                    var key = kvp.Key;
                    string value = string.Empty;

                    if (!File.Exists(kvp.Value))
                    {
                        switch (kvp.Key)
                        {
                            case ConfigurationSource.ProjectConfigs:
                                value = JsonConvert.SerializeObject(new ProjectConfigurations(), settings);
                                break;
                            case ConfigurationSource.SceneConfigs:
                                value = JsonConvert.SerializeObject(new SceneConfiguration(), settings);
                                break;
                            case ConfigurationSource.ExplorerConfigs:
                                value = JsonConvert.SerializeObject(new ExplorerConfigurations(), settings);
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
            var ser = JsonConvert.SerializeObject(s);
            configsCache[source] = ser;
            await File.WriteAllTextAsync(configsSource[source], ser);
        }

        public string GetConfiguration(ConfigurationSource source) => configsCache[source];
        public T GetConfiguration<T>(ConfigurationSource source)
        {
            T des = JsonConvert.DeserializeObject<T>(configsCache[source], settings);
            return des;
        }
    }

    public class ProjectConfigurations
    {
        public string AssemblyName { get; set; } = "CSharp_Assembly";
        public BuildType BuildType { get; set; } = BuildType.Debug;
    }
    public class ExplorerConfigurations
    {
        public List<string> ExcludeExtension { get; set; }

        public ExplorerConfigurations()
        {
            ExcludeExtension = new List<string>() { ".exe", MetadataManager.META_EXTENSION };
        }
    }

    public class SceneConfiguration
    {
        
    }
}
