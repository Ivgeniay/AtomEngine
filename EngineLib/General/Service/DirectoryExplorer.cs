using System.Collections.Concurrent;

namespace EngineLib
{
    public class DirectoryExplorer : IService
    {
        protected ConcurrentDictionary<Type, string> paths = new ConcurrentDictionary<Type, string>();
        protected bool _isInitialize = false;

        public string GetPath(Type directoryType) => paths[directoryType];
        public string GetPath<T>() where T : DirectoryType => GetPath(typeof(T));

        public void ResisterPath<T>(string path) where T : DirectoryType =>
            paths.AddOrUpdate(typeof(T), path, (e1, e2) => path);

        public virtual Task InitializeAsync()
        {
            if (_isInitialize) return Task.CompletedTask;

            return Task.Run(() =>
            {
                foreach (var path in paths.Values)
                {
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }

                _isInitialize = true;
            });
        }
    }

    public class DirectoryType { }
}
