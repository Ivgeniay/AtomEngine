using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Editor
{
    public static class Configuration
    {
        private static Dictionary<ConfigurationSource, string> configsSource = new Dictionary<ConfigurationSource, string>();
        private static Dictionary<ConfigurationSource, string> configsCache = new Dictionary<ConfigurationSource, string>();

        private const string EXPLORER_CONFIG_FILE = "explorer.config";
        private const string SCENE_CONFIG_FILE = "scenes.config";

        static Configuration()
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

            foreach (KeyValuePair<ConfigurationSource, string> kvp in configsSource)
            {
                var key = kvp.Key;
                string value = string.Empty;

                if (!File.Exists(kvp.Value))
                {
                    using (FileStream file = File.Create(kvp.Value)) {
                    }
                }
                else
                {
                    value = File.ReadAllText(kvp.Value);
                }
                configsCache.Add(key, value);
            }
        }

        public static async void SafeConfiguration(ConfigurationSource source, object s)
        {
            var ser = JsonConvert.SerializeObject(s);
            configsCache[source] = ser;
            await File.WriteAllTextAsync(configsSource[source], ser);
        }

        public static string GetConfiguration(ConfigurationSource source) => configsCache[source];
        public static T GetConfiguration<T>(ConfigurationSource source)
        {
            T des = JsonConvert.DeserializeObject<T>(configsCache[source]);
            return des;
        }
    }
}
