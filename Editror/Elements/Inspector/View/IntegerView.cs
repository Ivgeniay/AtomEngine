using AtomEngine;
using Avalonia.Controls;
using EngineLib;
using System;
using System.Linq;

namespace Editor
{
    internal class IntegerView : BasePropertyView
    {
        public IntegerView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            int value = (int)descriptor.Value;

            IntegerField field = new IntegerField();
            field.Label = descriptor.Name;
            field.IsReadOnly = descriptor.IsReadOnly;
            field.Value = value;
            field.ValueChanged += (s, e) =>
            {
                if (field.Value != value)
                {
                    Validation(field);
                }
            };
            Validation(field, true);

            return field;
        }

        private void Validation(IntegerField field, bool isFirstValidation = false)
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
                        var attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MaxAttribute));
                        if (attribute != null)
                        {
                            var maxAttribute = (MaxAttribute)attribute;
                            if (field.Value > maxAttribute.MaxValue)
                            {
                                field.Value = (int)maxAttribute.MaxValue;
                                if (field.Value < 0) field.Value = 0;
                                descriptor.OnValueChanged?.Invoke((uint)field.Value);
                                isCalledYet = true;
                            }
                        }

                        attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MinAttribute));
                        if (attribute != null)
                        {
                            var minAttribute = (MinAttribute)attribute;
                            if (field.Value < minAttribute.MinValue)
                            {
                                field.Value = (int)minAttribute.MinValue;
                                if (field.Value < 0) field.Value = 0;
                                descriptor.OnValueChanged?.Invoke((uint)field.Value);
                                isCalledYet = true;
                            }
                        }
                    }
                }
            }

            if (!isFirstValidation && !isCalledYet)
            {
                descriptor.OnValueChanged?.Invoke((uint)field.Value);
            }
        }
    }

    internal class UIntegerView : BasePropertyView
    {
        public UIntegerView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            uint value = (uint)descriptor.Value;

            UIntegerField field = new UIntegerField();
            field.Label = descriptor.Name;
            field.IsReadOnly = descriptor.IsReadOnly;
            field.Value = value;
            field.ValueChanged += (s, e) =>
            {
                if (field.Value != value)
                {
                    Validation(field);
                }
            };
            Validation(field, true);

            return field;
        }

        private void Validation(UIntegerField field, bool isFirstValidation = false)
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
                        var attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MaxAttribute));
                        if (attribute != null)
                        {
                            var maxAttribute = (MaxAttribute)attribute;
                            if (field.Value > maxAttribute.MaxValue)
                            {
                                field.Value = (uint)maxAttribute.MaxValue;
                                descriptor.OnValueChanged?.Invoke(field.Value);
                                isCalledYet = true;
                            }
                        }

                        attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MinAttribute));
                        if (attribute != null)
                        {
                            var minAttribute = (MinAttribute)attribute;
                            if (field.Value < minAttribute.MinValue)
                            {
                                field.Value = (uint)minAttribute.MinValue;
                                descriptor.OnValueChanged?.Invoke(field.Value);
                                isCalledYet = true;
                            }
                        }
                    }
                }
            }

            if (field.Value < 0)
            {
                field.Value = 0;
            }

            if (!isFirstValidation && !isCalledYet)
            {
                descriptor.OnValueChanged?.Invoke(field.Value);
            }
        }
    }
}
