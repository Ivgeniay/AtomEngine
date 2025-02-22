using System.Collections.Generic;
using System;

namespace Editor
{
    public static class Select
    {
        public static event Action<EntityHierarchyItem> OnSelect;
        public static event Action<EntityHierarchyItem> OnDeSelect;

        private static List<EntityHierarchyItem> _selected = new List<EntityHierarchyItem>();
        public static IEnumerable<EntityHierarchyItem> Selected { get { return _selected; } }

        internal static void SelectItem(EntityHierarchyItem selected)
        {
            if (!_selected.Contains(selected))
            {
                _selected.Add(selected);
                OnSelect?.Invoke(selected);
            }
        }

        internal static void DeSelect(EntityHierarchyItem entity)
        {
            if (_selected.Contains(entity))
            {
                _selected.Remove(entity);
                OnDeSelect?.Invoke(entity);
            }
        }
    
        internal static void DeSelectAll()
        {
            List<EntityHierarchyItem> temp = new();

            foreach (var item in _selected) 
                temp.Add(item);

            temp.ForEach(e => DeSelect(e));
            temp.Clear();
        }
    }
}
