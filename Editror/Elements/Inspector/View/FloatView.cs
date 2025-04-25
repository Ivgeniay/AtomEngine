using AtomEngine;
using Avalonia.Controls;
using EngineLib;
using System;
using System.Linq;

namespace Editor
{
    internal class FloatView : BasePropertyView
    {
        public FloatView(PropertyDescriptor descriptor) : base(descriptor) { }



        public override Control GetView()
        {
            float value = (float)descriptor.Value;

            FloatField field = new FloatField();

            field.IsReadOnly = descriptor.IsReadOnly;
            field.Label = descriptor.Name;

            field.Value = value;

            field.ValueChanged += (sender, e) =>
            {
                if (field.Value != null)
                {
                    Validation(field);
                }
            };
            Validation(field, true);

            //if (descriptor.Context is EntityInspectorContext context)
            //{
            //    var observer = new ComponentFieldObserver<float>(
            //        context.EntityId,
            //        context.Component,
            //        descriptor.Name,
            //        newValue =>
            //        {
            //            if (Math.Abs(field.Value - newValue) > float.Epsilon)
            //            {
            //                field.Value = newValue;
            //            }
            //        });

            //    RegisterObserver(observer);
            //}

            return field;
        }

        private void Validation(FloatField field, bool isFirstValidation = false)
        {
            bool isCalledYet = false;

            if (descriptor.Context is EntityInspectorContext context)
            {
                Type type = context.Component.GetType();
                var _field = type.GetField(descriptor.Name);
                if (_field != null)
                {
                    var attributes = _field.GetCustomAttributes(false);
                    if (attributes != null && attributes.Count() > 0)
                    {
                        object attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MinAttribute));
                        if (attribute != null)
                        {
                            var minAttribute = (MinAttribute)attribute;
                            if (field.Value < minAttribute.MinValue)
                            {
                                field.Value = (float)minAttribute.MinValue;
                                descriptor.OnValueChanged?.Invoke(field.Value);
                                isCalledYet = true;
                            }
                        }

                        attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MaxAttribute));
                        if (attribute != null && !isCalledYet)
                        {
                            var maxAttribute = (MaxAttribute)attribute;
                            if (field.Value > maxAttribute.MaxValue)
                            {
                                field.Value = (float)maxAttribute.MaxValue;
                                descriptor.OnValueChanged?.Invoke(field.Value);
                                isCalledYet = true;
                            }
                        }
                    }
                }
            }

            if (!isFirstValidation && !isCalledYet)
            {
                descriptor.OnValueChanged?.Invoke(field.Value);
            }
        }
    }
}
