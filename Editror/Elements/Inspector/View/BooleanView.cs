using Avalonia.Controls;
using System;

namespace Editor
{
    internal class BooleanView : BasePropertyView
    {
        public BooleanView(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var checkBox = new CheckBox
            {
                IsChecked = Convert.ToBoolean(descriptor.Value),
                IsEnabled = !descriptor.IsReadOnly
            };

            checkBox.IsCheckedChanged += (s, e) =>
            {
                if (checkBox.IsChecked != null)
                {
                    descriptor.OnValueChanged?.Invoke(checkBox.IsChecked.Value);
                }
            };

            Grid.SetColumn(checkBox, 1);
            grid.Children.Add(checkBox);

            return grid;
        }
    }
}
