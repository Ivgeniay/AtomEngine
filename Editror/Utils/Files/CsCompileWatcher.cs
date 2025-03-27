using System.Threading.Tasks;
using EngineLib;

namespace Editor
{
    public class CsCompileWatcher : IService
    {
        private FileSystemWatcher watcher;
        private ScriptSyncSystem scriptSyncSystem;
        private bool _isWatchngProcess =true;

        public Task InitializeAsync()
        {
            scriptSyncSystem = ServiceHub.Get<ScriptSyncSystem>();
            watcher = ServiceHub.Get<FileSystemWatcher>();
            watcher.AssetChanged += FileCreatedHandler;
            return Task.CompletedTask;
        }

        private async void FileCreatedHandler(FileChangedEvent @event)
        {
            if (!_isWatchngProcess) return;

            if (@event.FileExtension == ".cs")
            {
                ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                await scriptSyncSystem.RebuildProject(pConf.BuildType);
            }
        }

        public void EnableWatching(bool value) => _isWatchngProcess = value;
    }
}
