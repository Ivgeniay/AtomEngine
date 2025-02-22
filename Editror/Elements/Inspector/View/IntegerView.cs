using Avalonia.Controls;
using System;

namespace Editor
{
    internal class IntegerView : BasePropertyView
    {
        public IntegerView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var numericUpDown = new NumericUpDown
            {
                Value = Convert.ToInt32(Descriptor.Value),
                IsEnabled = !Descriptor.IsReadOnly,
                Classes = { "vectorEditor" },
            };

            numericUpDown.ValueChanged += (s, e) =>
            {
                if (numericUpDown.Value != null)
                {
                    Descriptor.OnValueChanged?.Invoke(numericUpDown.Value);
                }
            };

            Grid.SetColumn(numericUpDown, 1);
            grid.Children.Add(numericUpDown);

            return grid;
        }
    }
}
