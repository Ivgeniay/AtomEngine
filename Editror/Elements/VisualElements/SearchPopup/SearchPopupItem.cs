namespace Editor
{
    public class SearchPopupItem
    {
        public string DisplayName { get; set; }
        public string Subtitle { get; set; }
        public string IconText { get; set; }
        public string Category { get; set; }
        public string[] SearchTags { get; set; }
        public object Value { get; set; }

        public SearchPopupItem(string displayName, object value)
        {
            DisplayName = displayName;
            Value = value;
        }

        public SearchPopupItem(string displayName, object value, string iconText = null, string category = null, string subtitle = null, string[] searchTags = null)
        {
            DisplayName = displayName;
            Value = value;
            IconText = iconText;
            Category = category;
            Subtitle = subtitle;
            SearchTags = searchTags;
        }
    }
}