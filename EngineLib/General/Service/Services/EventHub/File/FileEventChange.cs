namespace EngineLib
{
    public class FileEventChange : FileEventEH
    {
        public FileEventChange(WatcherChangeTypes changeType, string fullPath, string? name) : base(changeType, fullPath, name)
        { }
    }
}
