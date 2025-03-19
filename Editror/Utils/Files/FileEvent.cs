using Newtonsoft.Json;


namespace Editor
{
    public class FileEvent
    {
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string FileFullPath { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    public class FileSelectionEvent : FileEvent
    {
    }

    public class FileChangedEvent : FileEvent
    {
    }

    public class FileCreateEvent : FileEvent
    {
    }

    public class DragDropEventArgs : FileEvent
    {
        //public ExpandableFileItemChild ChildItem { get; set; }
        public object Context { get; set; } = null;
    }
}