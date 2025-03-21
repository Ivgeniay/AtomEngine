using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AtomEngine;
using System.IO;
using System;
using EngineLib;

namespace Editor
{
    public class ModelManager : IService
    {
        public string[] _meshExtensionsPattern = new string[] { "*.obj" };
        private Dictionary<string, string> _guidPathMap = new Dictionary<string, string>();
        private Dictionary<string, string> _cacheMeshes = new Dictionary<string, string>();

        public Task InitializeAsync()
        {
            return Task.Run(() => {
                string assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                CacheAllModel(assetsPath);
            });
        }

        public IEnumerable<string> GetExtensions()
        {
            foreach (string extension in _meshExtensionsPattern)
            {
                yield return extension.Substring(1);
            }
        }

        public string LoadModel(string path)
        {
            if (!File.Exists(path))
            {
                if (_cacheMeshes.TryGetValue(path, out string mat)) _cacheMeshes.Remove(path);

                DebLogger.Error($"File {path} is not exist");
                return null;
            }

            if (_cacheMeshes.TryGetValue(path, out string meshText))
            {
                return meshText;
            }

            var metadata = ServiceHub.Get<MetadataManager>().GetMetadata(path);

            string sourceText = File.ReadAllText(path);
            _cacheMeshes[path] = sourceText;
            _guidPathMap[metadata.Guid] = path;

            return sourceText;
        }
        public string? GetPath(string guid)
        {
            return _guidPathMap[guid];
        }
        public string? GetGuid(string path)
        {
            return _guidPathMap.FirstOrDefault(e => e.Value == path).Value;
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
