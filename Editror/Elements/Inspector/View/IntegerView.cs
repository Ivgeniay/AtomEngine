using Avalonia.Controls;
using System;

namespace Editor
{
    internal class IntegerView : BasePropertyView
    {
        public IntegerView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            int value = (int)descriptor.Value;

            IntegerField field = new IntegerField();
            field.IsReadOnly = descriptor.IsReadOnly;
            field.Value = value;
            field.ValueChanged += (s, e) =>
            {
                if (field.Value != value)
                {
                    descriptor.OnValueChanged?.Invoke(field.Value);
                }
            };


            return field;

            //var grid = CreateBaseLayout();

            //var numericUpDown = new NumericUpDown
            //{
            //    Value = Convert.ToInt32(descriptor.Value),
            //    IsEnabled = !descriptor.IsReadOnly,
            //    Classes = { "vectorEditor" },
            //};

            //numericUpDown.ValueChanged += (s, e) =>
            //{
            //    if (numericUpDown.Value != null)
            //    {
            //        descriptor.OnValueChanged?.Invoke(numericUpDown.Value);
            //    }
            //};

            //Grid.SetColumn(numericUpDown, 1);
            //grid.Children.Add(numericUpDown);

            //return grid;
        }
    }
}
