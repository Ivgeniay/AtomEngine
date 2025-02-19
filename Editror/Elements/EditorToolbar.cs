using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;

namespace Editor
{
    public class EditorToolbar
    {
        private Border _container;
        private Action<string> _menuItemClickHandler;
        private Dictionary<string, List<string>> _menuItems;

        public EditorToolbar(Border container, Action<string> menuItemClickHandler)
        {
            _container = container;
            _menuItemClickHandler = menuItemClickHandler;

            if (_container == null)
            {
                throw new ArgumentNullException(nameof(container), "Toolbar container cannot be null");
            }

            InitializeMenuItems();
            CreateToolbar();
        }

        private void InitializeMenuItems()
        {
            _menuItems = new Dictionary<string, List<string>>
            {
                { "File", new List<string> { "New", "Open", "Save", "Save As...", "Exit" } },
                { "Edit", new List<string> { "Undo", "Redo", "Cut", "Copy", "Paste", "Delete" } },
                { "View", new List<string> { "Project Explorer", "Properties", "Console", "Output" } },
                { "Build", new List<string> { "Build Project", "Build Solution", "Clean", "Rebuild All" } },
                { "Tools", new List<string> { "Options", "Extensions", "Package Manager" } },
                { "Help", new List<string> { "Documentation", "About" } }
            };
        }

        private void CreateToolbar()
        {
            if (_container == null) return;
            var toolbarBackground = new Border
            {
                Classes = { "toolbarBackground" },
                Height = 28
            };

            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Margin = new Thickness(5, 0, 0, 0)
            };

            foreach (var menuCategory in _menuItems.Keys)
            {
                var menuButton = CreateMenuButton(menuCategory, _menuItems[menuCategory]);
                toolbarPanel.Children.Add(menuButton);
            }

            toolbarBackground.Child = toolbarPanel;
            _container.Child = toolbarBackground;
        }

        private Button CreateMenuButton(string categoryName, List<string> items)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                categoryName = "Unknown";
            }

            if (items == null)
            {
                items = new List<string>();
            }

            var button = new Button
            {
                Content = categoryName,
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

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item)) continue;

                var menuItem = new Button
                {
                    Content = item,
                    Classes = { "menuItem" },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(8, 6)
                };

                menuItem.Click += (s, e) =>
                {
                    _menuItemClickHandler?.Invoke(item);
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