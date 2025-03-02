using Avalonia.Controls;
using System.Collections.Generic;
using Avalonia;

namespace Editor
{
    internal class ComponentPropertiesView : BasePropertyView
    {
        public ComponentPropertiesView(PropertyDescriptor descriptor) : base(descriptor) { }
        public override Control GetView()
        {
            var panel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };

            panel.Children.Add(new TextBlock
            {
                Text = descriptor.Name,
                Classes = { "componentHeader" }
            });

            var properties = (List<PropertyDescriptor>)descriptor.Value;
            foreach (var property in properties)
            {
                var view = InspectorViewFactory.CreateView(property);
                panel.Children.Add(view.GetView());
            }

            return panel;
        }
    }
}
