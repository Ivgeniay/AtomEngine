using Avalonia.Controls;

namespace Editor
{
    internal class StringView : BasePropertyView
    {
        public StringView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var textBox = new TextBox
            {
                Text = Descriptor.Value?.ToString() ?? string.Empty,
                IsReadOnly = Descriptor.IsReadOnly,
                Classes = { "propertyEditor" }
            };

            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);

            return grid;
        }
    }
}
