using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;

namespace Editor
{
    public class DirectoryExplorer : IService
    {
        private Dictionary<DirectoryType, string> paths = new Dictionary<DirectoryType, string>();
        private bool _isInitialize = false;

        public string GetPath(DirectoryType directoryType) => paths[directoryType];

        public Task Initialize()
        {
            if (_isInitialize) return Task.CompletedTask;

            return Task.Run(() =>
            {
                paths.Add(DirectoryType.Base, AppContext.BaseDirectory);
                paths.Add(DirectoryType.Plugins, Path.Combine(paths[DirectoryType.Base], "Plugins"));
                paths.Add(DirectoryType.Assets, Path.Combine(paths[DirectoryType.Base], "Assets"));
                paths.Add(DirectoryType.Configurations, Path.Combine(paths[DirectoryType.Base], "Configurations"));
                paths.Add(DirectoryType.CSharp_Assembly, Path.Combine(paths[DirectoryType.Base], "Project"));


                foreach (var path in paths.Values)
                {
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
            });
        }
    }
}
