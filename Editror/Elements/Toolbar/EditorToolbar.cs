using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;

namespace Editor
{
    internal class EditorToolbar
    {
        private Border _container;
        private List<EditorToolbarCategory> _categories = new List<EditorToolbarCategory>();
        private StackPanel toolbarPanel;

        public EditorToolbar(Border container)
        {
            _container = container;

            if (_container == null)
            {
                throw new ArgumentNullException(nameof(container), "Toolbar container cannot be null");
            }

            CreateToolbar();
        }


        private void CreateToolbar()
        {
            if (_container == null) return;
            var toolbarBackground = new Border
            {
                Classes = { "toolbarBackground" },
            };

            toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
            };


            toolbarBackground.Child = toolbarPanel;
            _container.Child = toolbarBackground;

            UpdateToolbarButtons();
        }

        public void RegisterCathegory(EditorToolbarCategory editorToolbarCategory)
        {
            if (!_categories.Contains(editorToolbarCategory))
                _categories.Add(editorToolbarCategory);

            UpdateToolbarButtons();
        }

        public void UpdateToolbarButtons()
        {
            toolbarPanel.Children.Clear();
            foreach (var category in _categories)
            {
                var menuButton = CreateMenuButtonsFromCategory(category);
                toolbarPanel.Children.Add(menuButton);
            }
        }
        private Button CreateMenuButtonsFromCategory(EditorToolbarCategory category)
        {
            var button = new Button
            {
                Content = category.Title,
                Classes = { "menuButton" },
                Padding = new Thickness(8, 4),
                Margin = new Thickness(1),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var flyout = new Flyout();
            var menuItemsPanel = new StackPanel
            {
                Spacing = 2,
                Width = 200
            };

            foreach (EditorToolbarButton item in category.Buttons)
            {
                var menuItem = new Button
                {
                    Content = item.Text,
                    Classes = { "menuItem" },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(8, 6)
                };

                menuItem.Click += (s, e) =>
                {
                    item?.Action?.Invoke();
                    flyout.Hide();
                };

                menuItemsPanel.Children.Add(menuItem);
            }

            flyout.Content = menuItemsPanel;
            FlyoutBase.SetAttachedFlyout(button, flyout);

            button.Click += (s, e) =>
            {
                FlyoutBase.ShowAttachedFlyout(button);
            };

            return button;
        }
    }

}