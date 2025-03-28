namespace EngineLib
{
    public class FileEventCreated : FileEvent
    {
        public FileEventCreated(WatcherChangeTypes changeType, string fullPath, string? name) : base(changeType, fullPath, name)
        { }
    }
}
