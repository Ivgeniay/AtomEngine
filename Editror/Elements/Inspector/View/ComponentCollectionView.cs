using System.Collections.Generic;
using Avalonia.Controls;

namespace Editor
{
    internal class ComponentCollectionView : BasePropertyView
    {
        public ComponentCollectionView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var panel = new StackPanel();

            var components = (List<IInspectable>)Descriptor.Value;
            foreach (var componentInspectable in components)
            {
                // Создаем заголовок компонента
                var header = new TextBlock
                {
                    Text = componentInspectable.Title,
                    Classes = { "componentHeader" }
                };
                panel.Children.Add(header);

                // Создаем view для всех свойств компонента
                foreach (var property in componentInspectable.GetProperties())
                {
                    var view =InspectorViewFactory.CreateView(property);
                    panel.Children.Add(view.GetView());
                }

                // Разделитель между компонентами
                panel.Children.Add(new Separator());
            }

            return panel;
        }
    }
}
