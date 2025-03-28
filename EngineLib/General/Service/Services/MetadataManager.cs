using System.Security.Cryptography;

namespace EngineLib
{
    public abstract class MetadataManager : IService
    {
        public const string META_EXTENSION = ".meta";

        protected Dictionary<string, FileMetadata> _metadataCache = new();
        protected Dictionary<string, string> _guidToPathMap = new();
        protected Dictionary<string, MetadataType> _extensionToTypeMap = new();

        protected EventHub eventHub;

        protected bool _isInitialized = false;

        protected void InitializeExtensionMappings()
        {
            _extensionToTypeMap[".png"] = MetadataType.Texture;
            _extensionToTypeMap[".jpg"] = MetadataType.Texture;
            _extensionToTypeMap[".jpeg"] = MetadataType.Texture;
            _extensionToTypeMap[".bmp"] = MetadataType.Texture;
            _extensionToTypeMap[".tga"] = MetadataType.Texture;
            _extensionToTypeMap[".gif"] = MetadataType.Texture;

            _extensionToTypeMap[".fbx"] = MetadataType.Model;
            _extensionToTypeMap[".obj"] = MetadataType.Model;
            _extensionToTypeMap[".blend"] = MetadataType.Model;
            _extensionToTypeMap[".3ds"] = MetadataType.Model;

            _extensionToTypeMap[".wav"] = MetadataType.Audio;
            _extensionToTypeMap[".mp3"] = MetadataType.Audio;
            _extensionToTypeMap[".ogg"] = MetadataType.Audio;

            _extensionToTypeMap[".shader"] = MetadataType.Shader;

            _extensionToTypeMap[".glsl"] = MetadataType.ShaderSource;
            _extensionToTypeMap[".hlsl"] = MetadataType.ShaderSource;

            _extensionToTypeMap[".cs"] = MetadataType.Script;

            _extensionToTypeMap[".scene"] = MetadataType.Scene;
            _extensionToTypeMap[".prefab"] = MetadataType.Prefab;

            _extensionToTypeMap[".mat"] = MetadataType.Material;

            _extensionToTypeMap[".asset"] = MetadataType.Asset;

            _extensionToTypeMap[".json"] = MetadataType.Data;
            _extensionToTypeMap[".xml"] = MetadataType.Data;
            _extensionToTypeMap[".txt"] = MetadataType.Text;
        }


        public virtual Task InitializeAsync()
        {
            eventHub = ServiceHub.Get<EventHub>();
            InitializeExtensionMappings();

            return Task.CompletedTask;
        }

        public abstract FileMetadata GetMetadataByGuid(string guid);
        public abstract FileMetadata GetMetadata(string filePath);
        public abstract string GetPathByGuid(string guid);

        public abstract FileMetadata CreateMetadata(string filePath);
        public abstract void SaveMetadata(string filePath, FileMetadata metadata);
        public abstract void SaveMetadata(FileMetadata metadata);
        public abstract FileMetadata LoadMetadata(string metaFilePath);

        public virtual void CacheMetadata(FileMetadata metadata, string filePath, bool withInvoke = true)
        {
            SaveMetadata(filePath, metadata);
            _metadataCache[filePath] = metadata;
            _guidToPathMap[metadata.Guid] = filePath;

            if (withInvoke)
                eventHub.SendEvent<MetadataCachedEvent>(new MetadataCachedEvent
                {
                    Metadata = metadata,
                });
        }


        public virtual List<string> FindDependentAssets(string guid)
        {
            var dependentAssets = new List<string>();

            foreach (var entry in _metadataCache)
            {
                if (entry.Value.Dependencies.Contains(guid))
                {
                    dependentAssets.Add(entry.Key);
                }
            }

            return dependentAssets;
        }
        public virtual List<string> FindAssetsByType(MetadataType assetType)
        {
            return _metadataCache
                .Where(kv => kv.Value.AssetType.Equals(assetType))
                .Select(kv => kv.Key)
                .ToList();
        }
        public virtual List<string> FindAssetsByTag(string tag)
        {
            return _metadataCache
                .Where(kv => kv.Value.Tags.Contains(tag))
                .Select(kv => kv.Key)
                .ToList();
        }
        public virtual MetadataType GetTypeByExtension(string extension)
        {
            return _extensionToTypeMap[extension];
        }


        public virtual string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public virtual void AddDependency(string filePath, string dependencyGuid)
        {
            var metadata = GetMetadata(filePath);

            if (!metadata.Dependencies.Contains(dependencyGuid))
            {
                metadata.Dependencies.Add(dependencyGuid);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }
        public virtual void RemoveDependency(string filePath, string dependencyGuid)
        {
            var metadata = GetMetadata(filePath);

            if (metadata.Dependencies.Contains(dependencyGuid))
            {
                metadata.Dependencies.Remove(dependencyGuid);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }
        public virtual void AddTag(string filePath, string tag)
        {
            var metadata = GetMetadata(filePath);

            if (!metadata.Tags.Contains(tag))
            {
                metadata.Tags.Add(tag);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }
        public virtual void RemoveTag(string filePath, string tag)
        {
            var metadata = GetMetadata(filePath);

            if (metadata.Tags.Contains(tag))
            {
                metadata.Tags.Remove(tag);
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }

        public void UpdateImportSettings(string filePath, Dictionary<string, object> settings)
        {
            var metadata = GetMetadata(filePath);

            bool changed = false;
            foreach (var setting in settings)
            {
                if (!metadata.ImportSettings.ContainsKey(setting.Key) ||
                    !metadata.ImportSettings[setting.Key].Equals(setting.Value))
                {
                    metadata.ImportSettings[setting.Key] = setting.Value;
                    changed = true;
                }
            }

            if (changed)
            {
                metadata.Version++;
                metadata.LastModified = DateTime.UtcNow;
                SaveMetadata(filePath, metadata);
            }
        }
    }
}
