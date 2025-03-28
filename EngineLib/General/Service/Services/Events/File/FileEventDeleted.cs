namespace EngineLib
{
    public class FileEventDeleted : FileEvent
    {
        public FileEventDeleted(WatcherChangeTypes changeType, string fullPath, string? name) : base(changeType, fullPath, name)
        { }
    }
}
