using AtomEngine;
using Avalonia.Controls;
using System;

namespace Editor
{
    internal class FloatView : BasePropertyView
    {
        public FloatView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            float value = (float)descriptor.Value;

            FloatField field = new FloatField();
            field.Label = descriptor.Name;
            field.Value = value;

            field.ValueChanged += (sender, e) =>
            {
                if (field.Value != null)
                {
                    DebLogger.Debug($"KEK: {e}");
                    descriptor.OnValueChanged?.Invoke((float)field.Value);
                }
            };

            return field;
        }
    }
}
