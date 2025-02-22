using Avalonia.Controls;
using Avalonia.Media;

namespace Editor
{
    internal class UnsupportedTypeView : BasePropertyView
    {
        public UnsupportedTypeView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var textBlock = new TextBlock
            {
                Text = $"[Unsupported type: {Descriptor.Type}]",
                Foreground = Brushes.Gray,
                FontStyle = FontStyle.Italic
            };

            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            return grid;
        }
    }
}
