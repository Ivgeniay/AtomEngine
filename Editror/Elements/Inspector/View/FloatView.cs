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
            FloatField field = new FloatField();
            field.Label = descriptor.Name;

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
