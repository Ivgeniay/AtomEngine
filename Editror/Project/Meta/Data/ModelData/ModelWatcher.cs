using System.Threading.Tasks;
using System.Linq;
using System.IO;
using EngineLib;

namespace Editor
{
    internal class ModelWatcher : IService
    {
        private EventHub _eventHub;
        private EditorMetadataManager _metadataManager;

        public Task InitializeAsync()
        {
            _metadataManager = ServiceHub.Get<EditorMetadataManager>();

            _eventHub = ServiceHub.Get<EventHub>();
            //_eventHub.Subscribe<MetadataCreateEvent>(CreateMetadataEventHandler);
            _eventHub.Subscribe<MetadataCachedEvent>(CreateMetadataEventHandler);

            return Task.CompletedTask;
        }

        private void CreateMetadataEventHandler(MetadataCachedEvent metadataCreateEvent)
        {
            if (metadataCreateEvent == null || metadataCreateEvent.Metadata == null) return;
            if (metadataCreateEvent.Metadata.AssetType != MetadataType.Model) return;
            if (metadataCreateEvent.Metadata is not ModelMetadata modelData) return;

            var path = _metadataManager.GetPathByGuid(modelData.Guid);
            if (path == null) return;

            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var directoryPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
            var relativePath = Path.GetRelativePath(directoryPath, path);

            if (name == "torus")
            {

            }

            var result = MeshCompiler.TryToCompile(new FileEvent
            {
                FilePath = relativePath,
                FileName = name,
                FileExtension = extension,
                FileFullPath = path,
            });

            if (result != null && result.Success)
            {
                foreach(var mesh in result.ModelData.Meshes)
                {
                    modelData.Textures.AddRange(mesh.TextureInfos);
                }

                foreach (var kvpStringMeshNode in result.ModelData.NodeMap)
                {
                    NodeModelData nodeModelData = new NodeModelData
                    {
                        MeshPath = result.ModelData.GetNodePath(kvpStringMeshNode.Value),
                        MeshName = kvpStringMeshNode.Key,
                        Matrix = kvpStringMeshNode.Value.Transformation,
                    };

                    if (kvpStringMeshNode.Value.MeshIndices != null && kvpStringMeshNode.Value.MeshIndices.Count > 0)
                    {
                        nodeModelData.Index = kvpStringMeshNode.Value.MeshIndices[0];
                        if (kvpStringMeshNode.Value.MeshIndices.Count > 1)
                        {

                        }
                    }

                    modelData.MeshesData.Add(nodeModelData);
                }
            }

            _metadataManager.SaveMetadata(path, modelData);
        }
    }
}
