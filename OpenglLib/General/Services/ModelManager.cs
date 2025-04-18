using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class ModelManager : IService
    {
        public string[] _meshExtensionsPattern = new string[] { "*.obj" };
        protected Dictionary<string, string> _guidPathMap = new Dictionary<string, string>();
        protected Dictionary<string, string> _cacheMeshes = new Dictionary<string, string>();
        protected MetadataManager _metadataManager;
        public virtual Task InitializeAsync()
        {
            _metadataManager = ServiceHub.Get<MetadataManager>();
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
            if (!FileLoader.IsExist(path))
            {
                if (_cacheMeshes.TryGetValue(path, out string mat)) _cacheMeshes.Remove(path);

                DebLogger.Error($"File {path} is not exist");
                return null;
            }

            if (_cacheMeshes.TryGetValue(path, out string meshText))
            {
                return meshText;
            }

            var metadata = _metadataManager.GetMetadata(path);

            string sourceText = FileLoader.LoadFile(path);
            _cacheMeshes[path] = sourceText;
            _guidPathMap[metadata.Guid] = path;

            return sourceText;
        }
        public string? GetPath(string guid)
        {
            if (_guidPathMap.TryGetValue(guid, out string path))
            {
                return path;
            }

            path = _metadataManager.GetPathByGuid(guid);
            LoadModel(path);
            return _guidPathMap[guid];
        }
        public string? GetGuid(string path)
        {
            return _guidPathMap.FirstOrDefault(e => e.Value == path).Value;
        }

    }
}
