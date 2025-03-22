using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class ModelManager : IService
    {
        public string[] _meshExtensionsPattern = new string[] { "*.obj" };
        protected Dictionary<string, string> _guidPathMap = new Dictionary<string, string>();
        protected Dictionary<string, string> _cacheMeshes = new Dictionary<string, string>();

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
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

    }
}
