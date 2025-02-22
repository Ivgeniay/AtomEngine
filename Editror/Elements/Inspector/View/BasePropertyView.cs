using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Editor
{
    internal abstract class BasePropertyView : IInspectorView
    {
        protected readonly PropertyDescriptor Descriptor;

        protected BasePropertyView(PropertyDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public abstract Control GetView();


        protected Grid CreateBaseLayout()
        {
            var grid = new Grid
            {
                Margin = new Thickness(4, 0),
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(120) },
                        new ColumnDefinition { Width = GridLength.Star }
                    }
            };

            var label = new TextBlock
            {
                Text = Descriptor.Name,
                Classes = { "propertyLabel" }
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            return grid;
        }
    }
}
