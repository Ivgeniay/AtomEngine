namespace EngineLib
{
    public interface IDependencyHandler
    {
        void HandleDependencyChanged(string assetPath, string changedDependencyPath, FileMetadata dependencyMeta);
        void HandleDependencyDeleted(string assetPath, string deletedDependencyGuid, FileMetadata dependencyMeta);
        void HandleDependencyAdded(string assetPath, string addedDependencyPath, FileMetadata dependencyMeta);
        void HandleDependencyRemoved(string assetPath, string removedDependencyPath, FileMetadata dependencyMeta);
    }
}
