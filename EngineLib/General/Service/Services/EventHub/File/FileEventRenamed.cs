namespace EngineLib
{
    public class FileEventRenamed : FileEventEH
    {
        private readonly string? _oldName;
        private readonly string _oldFullPath;
        public string OldFullPath { get =>_oldFullPath; } 
        public string? OldName { get => _oldName; }

        public FileEventRenamed(WatcherChangeTypes changeType, string fullPath, string? name, string? oldName, string oldPath) : base(changeType, fullPath, name)
        {
            _oldName = oldName;
            _oldFullPath = oldPath;
        }
    }
}
