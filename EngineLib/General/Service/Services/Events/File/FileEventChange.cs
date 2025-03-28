namespace EngineLib
{
    public class FileEventChange : FileEvent
    {
        public FileEventChange(WatcherChangeTypes changeType, string fullPath, string? name) : base(changeType, fullPath, name)
        { }
    }
}
