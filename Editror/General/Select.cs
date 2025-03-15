using System.Collections.Generic;
using System;

namespace Editor
{
    public enum SelectType { Selected, Deselect };
    public static class Select
    {
        public static event Action<uint> OnSelect;
        public static event Action<uint> OnDeSelect;
        public static event Action<uint, SelectType> OnSelectChange;

        private static List<uint> _selected = new List<uint>();
        public static IEnumerable<uint> Selected { get { return _selected; } }

        internal static void SelectItem(uint selected)
        {
            if (!_selected.Contains(selected))
            {
                _selected.Add(selected);
                OnSelect?.Invoke(selected);
                OnSelectChange?.Invoke(selected, SelectType.Selected);
            }
        }

        internal static void DeSelect(uint entity)
        {
            if (_selected.Contains(entity))
            {
                _selected.Remove(entity);
                OnDeSelect?.Invoke(entity);
                OnSelectChange?.Invoke((uint)entity, SelectType.Deselect);
            }
        }
    
        internal static void DeSelectAll()
        {
            List<uint> temp = new();

            foreach (var item in _selected) 
                temp.Add(item);

            temp.ForEach(e => DeSelect(e));
            temp.Clear();
        }
    }
}
