using System.Collections.Generic;
using System;

namespace Editor
{
    public class SearchPopupItemCategoryComparer : IComparer<SearchPopupItem>
    {
        public int Compare(SearchPopupItem x, SearchPopupItem y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int categoryComparison = string.Compare(x.Category, y.Category, StringComparison.OrdinalIgnoreCase);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            return string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}