using System.Collections.Generic;
using System;


namespace Editor
{
    public class ExpandableFileItem
    {
        public event Action<ExpandableFileItemChild, DragDropEventArgs> ChildItemDragged;
        public Func<string, bool> CanExpand { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<string, IEnumerable<ExpandableFileItemChild>> GetChildItems { get; set; }
        public Action<ExpandableFileItemChild, DragDropEventArgs> OnChildItemDrag { get; set; }
        public void RaiseChildItemDragged(ExpandableFileItemChild child, DragDropEventArgs args)
        {
            ChildItemDragged?.Invoke(child, args);
            OnChildItemDrag?.Invoke(child, args);
        }
    }
}