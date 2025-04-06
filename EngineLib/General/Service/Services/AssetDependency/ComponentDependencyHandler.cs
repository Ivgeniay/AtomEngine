namespace EngineLib
{
    public abstract class ComponentDependencyHandler : IDependencyHandler
    {
        public abstract void HandleDependencyChanged(string assetPath, string changedDependencyPath, FileMetadata dependencyMeta);
        public abstract void HandleDependencyDeleted(string assetPath, string deletedDependencyGuid, FileMetadata dependencyMeta);
        public abstract void HandleDependencyAdded(string assetPath, string addedDependencyPath, FileMetadata dependencyMeta);
        public abstract void HandleDependencyRemoved(string assetPath, string removedDependencyPath, FileMetadata dependencyMeta);
    }


}
