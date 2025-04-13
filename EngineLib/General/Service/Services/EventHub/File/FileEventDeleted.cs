namespace EngineLib
{
    public class FileEventDeleted : FileEventEH
    {
        public FileEventDeleted(WatcherChangeTypes changeType, string fullPath, string? name) : base(changeType, fullPath, name)
        { }
    }
}
