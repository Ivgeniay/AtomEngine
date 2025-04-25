using Avalonia.Controls;
using System.Numerics;
using Silk.NET.Maths;
using System;

namespace Editor.Elements.Inspector.View
{
    internal class Vector3View : BasePropertyView
    {
        public Vector3View(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            var vector = (Vector3)descriptor.Value;
            Vector3FloatField field = new Vector3FloatField();

            field.IsReadOnly = descriptor.IsReadOnly;
            field.Label = descriptor.Name;
            field.Value = vector;
            field.ValueChanged += (s, e) =>
            {
                descriptor.OnValueChanged?.Invoke(e);
            };

            if (descriptor.Context is EntityInspectorContext context)
            {
                var observer = new ComponentFieldObserver<Vector3>(
                    context.EntityId,
                    context.Component,
                    descriptor.Name,
                    newValue =>
                    {
                        field.Value = newValue;
                    });

                RegisterObserver(observer);
            }

            return field;
        }
    }

    internal class Vector3SilkView : BasePropertyView
    {
        public Vector3SilkView(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            var vector = (Vector3D<float>)descriptor.Value;
            Vector3FloatSilkField field = new Vector3FloatSilkField();

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
