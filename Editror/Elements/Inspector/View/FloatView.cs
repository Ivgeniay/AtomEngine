using Avalonia.Controls;
using System;

namespace Editor
{
    internal class FloatView : BasePropertyView
    {
        public FloatView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var numericUpDown = new NumericUpDown
            {
                Value = Convert.ToDecimal(Descriptor.Value),
                Increment = 0.1M,
                Classes = { "vectorEditor" },
                IsEnabled = !Descriptor.IsReadOnly
            };

            numericUpDown.ValueChanged += (s, e) =>
            {
                if (numericUpDown.Value != null)
                {
                    Descriptor.OnValueChanged?.Invoke((float)numericUpDown.Value);
                }
            };

            Grid.SetColumn(numericUpDown, 1);
            grid.Children.Add(numericUpDown);

            return grid;
        }
    }
}
