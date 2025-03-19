using System;


namespace Editor
{
    public class DragDropEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileFullPath { get; set; }
        public ExpandableFileItemChild ChildItem { get; set; }
    }
}