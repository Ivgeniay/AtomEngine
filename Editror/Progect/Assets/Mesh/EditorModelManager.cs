using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AtomEngine;
using System.IO;
using System;
using EngineLib;
using OpenglLib;

namespace Editor
{
    public class EditorModelManager : ModelManager
    {
        public override Task InitializeAsync()
        {
            return Task.Run(async () => {
                string assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                CacheAllModel(assetsPath);

                await base.InitializeAsync();
            });
        }
        public void CacheAllModel(string rootDirectory)
        {
            try
            {
                List<string> meshFiles = new List<string>();
                foreach (string extension in _meshExtensionsPattern)
                {
                    meshFiles.AddRange(Directory.GetFiles(rootDirectory, extension, SearchOption.AllDirectories));
                }

                foreach (var meshFile in meshFiles)
                {
                    try
                    {
                        var model = LoadModel(meshFile);
                        if (model != null)
                        {
                            DebLogger.Debug($"Cached model: {meshFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Warn($"Failed to cache model {meshFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error while scanning for model: {ex.Message}");
            }
        }
    }
}
