namespace Editor
{
    public class FileEventCommand
    {
        public string FileExtension { get; set; } = string.Empty;
        public FileEventType Type = FileEventType.FileCreate;
        public Command<FileEvent> Command { get; set; }
    }


}
