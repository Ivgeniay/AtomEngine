using Avalonia.Controls;
using System.Collections.Generic;
using Avalonia;
using AtomEngine;
using System;
using Silk.NET.OpenAL;

namespace Editor
{
    internal class ComponentPropertiesView : BasePropertyView
    {
        InspectorViewFactory _inspectorViewFactory;
        public ComponentPropertiesView(PropertyDescriptor descriptor) : base(descriptor) {
            _inspectorViewFactory = ServiceHub.Get<InspectorViewFactory>();
        }

        public override Control GetView()
        {
            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            var panel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };

            var header = new StackPanel();
            header.Orientation = Avalonia.Layout.Orientation.Horizontal;
            header.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            header.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

            header.Children.Add(new TextBlock
            {
                Text = descriptor.Name,
                Classes = { "componentHeader" }
            });
            header.Children.Add(new Button
            {
                Content = "X",
                Width = 32,
                Height = 32,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Classes = { "closeButton" },
                Command = new Command(() =>
                {
                    EntityInspectorContext context = (EntityInspectorContext) descriptor.Context;
                    sceneManager.RemoveComponent(context.EntityId, context.Component.GetType());
                }),
            });
            
            panel.Children.Add(header);

            var properties = (List<PropertyDescriptor>)descriptor.Value;
            foreach (var property in properties)
            {
                var view = _inspectorViewFactory.CreateView(property);
                panel.Children.Add(view.GetView());

                property.OnValueChanged += (e) =>
                {
                    if (descriptor.Context != null)
                    {
                        var context = (EntityInspectorContext) descriptor.Context;
                        sceneManager.ComponentChange(context.EntityId, context.Component);
                    }
                };
            }

            return panel;
        }
    }
}
