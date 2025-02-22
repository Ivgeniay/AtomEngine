using Avalonia.Controls;
using Avalonia.Input;

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

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Descriptor.OnValueChanged?.Invoke(textBox.Text);
                    textBox.Focus();
                }
            };
            textBox.LostFocus += (s, e) =>
            {
                Descriptor.OnValueChanged?.Invoke(textBox.Text);
            };

            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);

            return grid;
        }
    }
}
