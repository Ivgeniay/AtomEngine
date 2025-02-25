using Avalonia.Controls;
using System.Linq;
using System;

namespace Editor
{
    internal class EnumView : BasePropertyView
    {
        public EnumView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var comboBox = new ComboBox
            {
                Classes = { "propertyEditor" },
                IsEnabled = !Descriptor.IsReadOnly,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            Type enumType = Descriptor.Type;
            var values = Enum.GetValues(enumType).Cast<object>().ToList();

            foreach (var value in values)
            {
                comboBox.Items.Add(value);
            }

            comboBox.SelectedItem = Descriptor.Value;
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem != null)
                {
                    Descriptor.OnValueChanged?.Invoke(comboBox.SelectedItem);
                }
            };

            Grid.SetColumn(comboBox, 1);
            grid.Children.Add(comboBox);

            return grid;
        }
    }
}
