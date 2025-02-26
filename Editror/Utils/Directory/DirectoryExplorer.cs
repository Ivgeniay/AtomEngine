using System.Collections.Generic;
using System.IO;
using System;

namespace Editor
{
    public static class DirectoryExplorer
    {
        private static Dictionary<DirectoryType, string> paths = new Dictionary<DirectoryType, string>();

        static DirectoryExplorer()
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
        }

        public static string GetPath(DirectoryType directoryType) => paths[directoryType];
    }
}
