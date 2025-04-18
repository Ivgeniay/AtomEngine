using System.Threading.Tasks;
using System.IO;
using EngineLib;
using System;

namespace Editor
{
    internal class EditorDirectoryExplorer : DirectoryExplorer
    {
        public override Task InitializeAsync()
        {
            if (_isInitialize) return Task.CompletedTask;

            return Task.Run(async () =>
            {
                ResisterPath<BaseDirectory>(AppContext.BaseDirectory);
                ResisterPath<PluginsDirectory>(Path.Combine(paths[typeof(BaseDirectory)], "Plugins"));
                ResisterPath<AssetsDirectory>(Path.Combine(paths[typeof(BaseDirectory)], "Assets"));
                ResisterPath<ConfigurationsDirectory>(Path.Combine(paths[typeof(BaseDirectory)], "Configurations"));
                ResisterPath<CSharp_AssemblyDirectory>(Path.Combine(paths[typeof(BaseDirectory)], "Project"));
                ResisterPath<CacheDirectory>(Path.Combine(paths[typeof(BaseDirectory)], "Cache"));
                ResisterPath<EmbeddedResourcesDirectory>(Path.Combine(paths[typeof(CacheDirectory)], "Resources"));
                ResisterPath<ExePathDirectory>(Path.Combine(paths[typeof(BaseDirectory)], "Execution"));

                await base.InitializeAsync();
            });
        }
    }
    internal class BaseDirectory : DirectoryType;
    internal class PluginsDirectory : DirectoryType;
    internal class AssetsDirectory : DirectoryType;
    internal class ConfigurationsDirectory : DirectoryType;
    internal class CSharp_AssemblyDirectory : DirectoryType;
    internal class CacheDirectory : DirectoryType;
    internal class ExePathDirectory : DirectoryType;
}
