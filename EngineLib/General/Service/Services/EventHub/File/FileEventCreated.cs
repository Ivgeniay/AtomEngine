namespace EngineLib
{
    public class FileEventCreated : FileEventEH
    {
        public FileEventCreated(WatcherChangeTypes changeType, string fullPath, string? name) : base(changeType, fullPath, name)
        { }
    }
}
