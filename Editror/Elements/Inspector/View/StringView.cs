using AtomEngine;
using Avalonia.Controls;
using System;
using System.Linq;
using Key = Avalonia.Input.Key;

namespace Editor
{
    internal class StringView : BasePropertyView
    {
        public StringView(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            string text = descriptor.Value as string;

            StringField field = new StringField();

            field.IsReadOnly = descriptor.IsReadOnly;
            field.Label = descriptor.Name;
            field.Text = text;
            field.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Validation(field);
                    //descriptor.OnValueChanged?.Invoke(field.Text);
                    field.Focus();
                }
            };
            field.LostFocus += (s, e) =>
            {
                descriptor.OnValueChanged?.Invoke(field.Text);
            };

            Validation(field, true);

            return field;
        }

        private void Validation(StringField field, bool isFirstValidation = false)
        {
            bool isCalledYet = false;

            if (descriptor.Context is EntityInspectorContext context)
            {
                Type type = context.Component.GetType();
                var _field = type.GetField(descriptor.Name, 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                var attributes = _field.GetCustomAttributes(false);
                if (attributes != null && attributes.Count() > 0)
                {
                    var attribute = attributes.FirstOrDefault(e => e.GetType() == typeof(MaxLengthAttribute));
                    if (attribute != null)
                    {
                        MaxLengthAttribute maxLengthAttribute = attribute as MaxLengthAttribute;
                        if (field.Text.Length > maxLengthAttribute.MaxLength)
                        {
                            field.Text = field.Text.Substring(0, maxLengthAttribute.MaxLength);
                            descriptor.OnValueChanged?.Invoke(field.Text);
                            isCalledYet = true;
                        }
                    }
                }
            }

            if (!isCalledYet && !isFirstValidation)
            {
                descriptor.OnValueChanged?.Invoke(field.Text);
            }
        }
    }


}
