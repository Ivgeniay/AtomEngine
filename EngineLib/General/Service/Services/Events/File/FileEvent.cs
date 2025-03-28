namespace EngineLib
{
    public class FileEvent : EventHubEvent
    {
        private readonly WatcherChangeTypes _changeType;
        private readonly string? _name;
        private readonly string _fullPath;
        public FileEvent(WatcherChangeTypes changeType, string fullPath, string? name)
        {
            _changeType = changeType;
            _name = name;
            _fullPath = fullPath;
        }
        public WatcherChangeTypes ChangeType { get => _changeType; }
        public string FullPath { get => _fullPath; }
        public string? Name { get => _name; }
    }

}
