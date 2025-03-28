using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using System.Linq;
using EngineLib;
using Avalonia;
using System;
using AtomEngine;
using System.Reflection;

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
            int contentHieght = 32;

            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            var panel = new StackPanel { 
                Margin = new Thickness(0, 5, 0, 5),
            };
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 2),
                Child = panel
            };
            EntityInspectorContext context = (EntityInspectorContext)descriptor.Context;
            bool isHideCrossButton = context
                .Component
                .GetType()
                .GetCustomAttributes(false)
                .Any(e => e.GetType() == typeof(HideCloseAttribute));

            var header = new Grid
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            var headerBorder = new Border
            {
                Padding = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#404040")),
                CornerRadius = new CornerRadius(3),
                Height = contentHieght,
                Child = header
            };

            var textBlock = new TextBlock
            {
                Text = descriptor.Name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(textBlock, 0);

            header.Children.Add(textBlock);

            if (!isHideCrossButton)
            {
                var button = new Button
                {
                    Content = "x",
                    Width = contentHieght,
                    Height = contentHieght,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Classes = { "closeButton" },
                    Command = new Command(() =>
                    {
                        if (ExceptionalCases(context)) return;
                        sceneManager.RemoveComponent(context.EntityId, context.Component.GetType());
                    })
                };
                Grid.SetColumn(button, 2);

                header.Children.Add(button);
            }


            panel.Children.Add(headerBorder);

            Type componentType = null;
            if (descriptor.Context is EntityInspectorContext entityContext)
                componentType = entityContext.Component.GetType();



            var properties = (List<PropertyDescriptor>)descriptor.Value;
            foreach (var property in properties)
            {
                bool withIgnoreSceneOnChanging = false;
                if (componentType != null)
                {
                    FieldInfo field =
                        componentType
                        .GetField(property.Name, 
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        withIgnoreSceneOnChanging = field.GetCustomAttribute<IgnoreChangingSceneAttribute>() != null;
                    }
                }

                var view = _inspectorViewFactory.CreateView(property);
                panel.Children.Add(view.GetView());

                property.OnValueChanged += (e) =>
                {
                    if (descriptor.Context != null)
                    {
                        sceneManager.ComponentChange(context.EntityId, context.Component, withIgnoreSceneOnChanging);
                    }
                };
            }

            return border;
        }

        private bool ExceptionalCases(EntityInspectorContext context)
        {
            if (context.Component.GetType() == typeof(TransformComponent))
            {
                var hasHierarhyComponent = SceneManager.EntityCompProvider.HasComponent<HierarchyComponent>(context.EntityId);
                if (hasHierarhyComponent)
                {
                    DebLogger.Warn("You cannot delete a TransformComponent while there is a HierarchyComponent on the entity.\nThis ensures that the model matrices are calculated correctly.");
                    return true;
                }
            }

            return false;
        }
    }
}
