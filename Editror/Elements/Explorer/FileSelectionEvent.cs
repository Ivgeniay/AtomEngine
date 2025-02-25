using Newtonsoft.Json;


namespace Editor
{
    public class FileSelectionEvent
    {
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}