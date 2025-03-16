using Avalonia.Controls;
using Silk.NET.Maths;
using System.Numerics;

namespace Editor.Elements.Inspector.View
{
    internal class Vector2View : BasePropertyView
    {
        public Vector2View(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            var vector = (Vector2)descriptor.Value;
            Vector2FloatField field = new Vector2FloatField();

            field.IsReadOnly = descriptor.IsReadOnly;
            field.Label = descriptor.Name;
            field.Value = vector;
            field.ValueChanged += (s, e) =>
            {
                descriptor.OnValueChanged?.Invoke(e);
            };

            return field;
        }
    }


    internal class Vector2SilkView : BasePropertyView
    {
        public Vector2SilkView(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            var vector = (Vector2D<float>)descriptor.Value;
            Vector2FloatSilkField field = new Vector2FloatSilkField();

            field.IsReadOnly = descriptor.IsReadOnly;
            field.Label = descriptor.Name;
            field.Value = vector;
            field.ValueChanged += (s, e) =>
            {
                descriptor.OnValueChanged?.Invoke(e);
            };

            return field;
        }
    }
}
