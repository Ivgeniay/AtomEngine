using System.Threading.Tasks;
using System.IO;

namespace Editor
{
    internal class ModelWatcher : IService
    {
        private EventHub _eventHub;
        private MetadataManager _metadataManager;

        public Task InitializeAsync()
        {
            _metadataManager = ServiceHub.Get<MetadataManager>();

            _eventHub = ServiceHub.Get<EventHub>();
            _eventHub.Subscribe<MetadataCreateEvent>(CreateMetadataEventHandler);

            return Task.CompletedTask;
        }

        private void CreateMetadataEventHandler(MetadataCreateEvent metadataCreateEvent)
        {
            if (metadataCreateEvent == null || metadataCreateEvent.Metadata == null) return;
            if (metadataCreateEvent.Metadata.AssetType != MetadataType.Model) return;
            if (metadataCreateEvent.Metadata is not ModelMetadata modelData) return;

            var path = _metadataManager.GetPathByGuid(modelData.Guid);
            if (path == null) return;

            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var directoryPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.Assets);
            var relativePath = Path.GetRelativePath(directoryPath, path);

            var result =MeshCompiler.TryToCompile(new FileEvent
            {
                FilePath = relativePath,
                FileName = name,
                FileExtension = extension,
                FileFullPath = path,
            });

            if (result != null && result.Success)
            {
                for( var i = 0; i< result.Model._texturesLoaded.Count; i++)
                {
                    modelData.Textures.Add(i.ToString());
                }

                foreach(var kvpStringMeshNode in result.Model.NodeMap)
                {
                    NodeModelData nodeModelData = new NodeModelData
                    {
                        MeshPath = result.Model.GetNodePath(kvpStringMeshNode.Value),
                        MeshName = kvpStringMeshNode.Key,
                        Matrix = kvpStringMeshNode.Value.Transformation
                    };

                    modelData.MeshesData.Add(nodeModelData);
                }
            }

            _metadataManager.SaveMetadata(path, modelData);
        }
    }
}
