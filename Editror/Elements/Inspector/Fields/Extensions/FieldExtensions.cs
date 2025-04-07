using Avalonia.Controls;
using Avalonia;

namespace Editor
{
    public static class FieldExtensions
    {
        public static void InitializeInspectorFieldLayout(this Grid grid)
        {
            grid.ColumnDefinitions.Clear();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.35, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.65, GridUnitType.Star) });
            grid.Margin = new Thickness(4, 0);
        }
    }
}