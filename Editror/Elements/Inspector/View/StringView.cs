using Avalonia.Controls;
using Avalonia.Input;

namespace Editor
{
    internal class StringView : BasePropertyView
    {
        public StringView(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            StringField field = new StringField();
            field.Label = descriptor.Name;

            field.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    descriptor.OnValueChanged?.Invoke(field.Text);
                    field.Focus();
                }
            };
            field.LostFocus += (s, e) =>
            {
                descriptor.OnValueChanged?.Invoke(field.Text);
            };

            return field;
        }
    }
}
