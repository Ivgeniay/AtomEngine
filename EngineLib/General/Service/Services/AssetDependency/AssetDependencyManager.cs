using AtomEngine;

namespace EngineLib
{
    public class AssetDependencyManager : IService
    {
        private MetadataManager _metadataManager;
        private EventHub _eventHub;

        private Dictionary<MetadataType, ResourceDependencyHandler> _resourceDependencyHandlers = new();
        private Dictionary<MetadataType, ComponentDependencyHandler> _componentDependencyHandlers = new();

        public Task InitializeAsync()
        {
            _metadataManager = ServiceHub.Get<MetadataManager>();
            _eventHub = ServiceHub.Get<EventHub>();

            _eventHub.Subscribe<MetadataDeletedEvent>(HandleAssetDeleted);
            _eventHub.Subscribe<MetadataChandedEvent>(HandleAssetChanged);

            return Task.CompletedTask;
        }

        public void RegisterDependencyHandler(MetadataType assetType, IDependencyHandler handler)
        {
            if (handler is ResourceDependencyHandler resourceHandler)
            {
                _resourceDependencyHandlers[assetType] = resourceHandler;
            }
            if (handler is ComponentDependencyHandler componentHandler)
            {
                _componentDependencyHandlers[assetType] = componentHandler;
            }
        }

        private void HandleAssetDeleted(MetadataDeletedEvent evt)
        {
            if (evt.Metadata == null) return;

            string guid = evt.Metadata.Guid;
            MetadataType assetType = evt.Metadata.AssetType;

            if (_componentDependencyHandlers.TryGetValue(assetType, out var componentHandler))
            {
                try
                {
                    componentHandler.HandleDependencyDeleted(null, guid, evt.Metadata);
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка в ComponentDependencyHandler при обработке удаления ассета ({guid}): {ex.Message}");
                }
            }

            if (_resourceDependencyHandlers.TryGetValue(assetType, out var resourceHandler))
            {
                var dependentAssets = _metadataManager.FindDependentAssets(guid);
                foreach (var dependentPath in dependentAssets)
                {
                    try
                    {
                        resourceHandler.HandleDependencyDeleted(dependentPath, guid, evt.Metadata);
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка в ResourceDependencyHandler при обработке удаления зависимости ({dependentPath} -> {guid}): {ex.Message}");
                    }
                }
            }
        }

        private void HandleAssetChanged(MetadataChandedEvent evt)
        {
            if (evt.Metadata == null) return;

            string guid = evt.Metadata.Guid;
            string filePath = _metadataManager.GetPathByGuid(guid);
            if (string.IsNullOrEmpty(filePath)) return;

            MetadataType assetType = evt.Metadata.AssetType;

            if (_componentDependencyHandlers.TryGetValue(assetType, out var componentHandler))
            {
                try
                {
                    componentHandler.HandleDependencyChanged(null, filePath, evt.Metadata);
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка в ComponentDependencyHandler при обработке изменения ассета ({guid}): {ex.Message}");
                }
            }

            if (_resourceDependencyHandlers.TryGetValue(assetType, out var resourceHandler))
            {
                var dependentAssets = _metadataManager.FindDependentAssets(guid);
                foreach (var dependentPath in dependentAssets)
                {
                    try
                    {
                        resourceHandler.HandleDependencyChanged(dependentPath, guid, evt.Metadata);
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка в ResourceDependencyHandler при обработке удаления зависимости ({dependentPath} -> {guid}): {ex.Message}");
                    }
                }
            }
        }

        public void AddDependencyFromPathByPath(string assetPath, string dependencyPath)
        {
            try
            {
                var assetMeta = _metadataManager.GetMetadata(assetPath);
                var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);

                _metadataManager.AddDependency(assetPath, dependencyMeta.Guid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler))
                {
                    handler.HandleDependencyAdded(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при добавлении зависимости ({assetPath} -> {dependencyPath}): {ex.Message}");
            }
        }
        public void AddDependencyFromPathByGuid(string assetPath, string dependencyGuid)
        {
            try
            {
                string dependencyPath = _metadataManager.GetPathByGuid(dependencyGuid);
                if (string.IsNullOrEmpty(dependencyPath))
                {
                    DebLogger.Warn($"Не удалось найти зависимость с GUID: {dependencyGuid}");
                    return;
                }

                var assetMeta = _metadataManager.GetMetadata(assetPath);
                var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);

                _metadataManager.AddDependency(assetPath, dependencyGuid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler))
                {
                    handler.HandleDependencyAdded(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при добавлении зависимости по GUID ({assetPath} -> {dependencyGuid}): {ex.Message}");
            }
        }
        public void AddDependencyFromGuidByPath(string assetGuid, string dependencyPath)
        {
            try
            {
                string assetPath = _metadataManager.GetPathByGuid(assetGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    DebLogger.Warn($"Не удалось найти ассет с GUID: {assetGuid}");
                    return;
                }

                var assetMeta = _metadataManager.GetMetadata(assetPath);
                var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);

                _metadataManager.AddDependency(assetPath, dependencyMeta.Guid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler))
                {
                    handler.HandleDependencyAdded(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при добавлении зависимости ({assetGuid} -> {dependencyPath}): {ex.Message}");
            }
        }
        public void AddDependencyFromGuidByGuid(string assetGuid, string dependencyGuid)
        {
            try
            {
                string assetPath = _metadataManager.GetPathByGuid(assetGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    DebLogger.Warn($"Не удалось найти ассет с GUID: {assetGuid}");
                    return;
                }

                string dependencyPath = _metadataManager.GetPathByGuid(dependencyGuid);
                if (string.IsNullOrEmpty(dependencyPath))
                {
                    DebLogger.Warn($"Не удалось найти зависимость с GUID: {dependencyGuid}");
                    return;
                }

                var assetMeta = _metadataManager.GetMetadata(assetPath);
                var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);

                _metadataManager.AddDependency(assetPath, dependencyGuid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler))
                {
                    handler.HandleDependencyAdded(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при добавлении зависимости по GUID ({assetGuid} -> {dependencyGuid}): {ex.Message}");
            }
        }
        public void RemoveDependencyFromPathByPath(string assetPath, string dependencyPath)
        {
            try
            {
                var assetMeta = _metadataManager.GetMetadata(assetPath);
                var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);

                _metadataManager.RemoveDependency(assetPath, dependencyMeta.Guid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler))
                {
                    handler.HandleDependencyRemoved(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при удалении зависимости ({assetPath} -> {dependencyPath}): {ex.Message}");
            }
        }
        public void RemoveDependencyFromPathByGuid(string assetPath, string dependencyGuid)
        {
            try
            {
                string dependencyPath = _metadataManager.GetPathByGuid(dependencyGuid);
                var assetMeta = _metadataManager.GetMetadata(assetPath);

                _metadataManager.RemoveDependency(assetPath, dependencyGuid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler) && !string.IsNullOrEmpty(dependencyPath))
                {
                    var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);
                    handler.HandleDependencyRemoved(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при удалении зависимости по GUID ({assetPath} -> {dependencyGuid}): {ex.Message}");
            }
        }
        public void RemoveDependencyFromGuidByPath(string assetGuid, string dependencyPath)
        {
            try
            {
                string assetPath = _metadataManager.GetPathByGuid(assetGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    DebLogger.Warn($"Не удалось найти ассет с GUID: {assetGuid}");
                    return;
                }

                var assetMeta = _metadataManager.GetMetadata(assetPath);
                var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);

                _metadataManager.RemoveDependency(assetPath, dependencyMeta.Guid);

                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler))
                {
                    handler.HandleDependencyRemoved(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при удалении зависимости ({assetGuid} -> {dependencyPath}): {ex.Message}");
            }
        }
        public void RemoveDependencyFromGuidByGuid(string assetGuid, string dependencyGuid)
        {
            try
            {
                string assetPath = _metadataManager.GetPathByGuid(assetGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    DebLogger.Warn($"Не удалось найти ассет с GUID: {assetGuid}");
                    return;
                }

                var assetMeta = _metadataManager.GetMetadata(assetPath);

                _metadataManager.RemoveDependency(assetPath, dependencyGuid);

                string dependencyPath = _metadataManager.GetPathByGuid(dependencyGuid);
                if (_resourceDependencyHandlers.TryGetValue(assetMeta.AssetType, out var handler) && !string.IsNullOrEmpty(dependencyPath))
                {
                    var dependencyMeta = _metadataManager.GetMetadata(dependencyPath);
                    handler.HandleDependencyRemoved(assetPath, dependencyPath, dependencyMeta);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при удалении зависимости по GUID ({assetGuid} -> {dependencyGuid}): {ex.Message}");
            }
        }
    }
}
