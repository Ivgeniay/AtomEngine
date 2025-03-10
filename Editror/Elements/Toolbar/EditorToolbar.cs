using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;

namespace Editor
{
    public class EditorToolbar : Control
    {
        private Border _container;
        private List<EditorToolbarCategory> _categories = new List<EditorToolbarCategory>();
        private Dictionary<EditorToolbarCategory, Flyout> _floyouts = new Dictionary<EditorToolbarCategory, Flyout>();
        private Dictionary<EditorToolbarCategory, StackPanel> _stack = new Dictionary<EditorToolbarCategory, StackPanel>();
        private StackPanel toolbarPanel;

        internal Action<object> OnClose { get; set; }

        internal EditorToolbar(Border container)
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

            UpdateToolbar();
        }

        public void RegisterCathegory(EditorToolbarCategory editorToolbarCategory)
        {
            if (!_categories.Contains(editorToolbarCategory))
            {
                _categories.Add(editorToolbarCategory);
                UpdateToolbar();
            }
        }

        public void UpdateToolbar()
        {
            toolbarPanel.Children.Clear();
            _floyouts.Clear();
            _stack.Clear();
            foreach (var category in _categories)
            {
                var menuButton = CreateMenuButtonsFromCategory(category);
                toolbarPanel.Children.Add(menuButton);
            }
        }
        public IEnumerable<EditorToolbarCategory> GetEditorData() => _categories;
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
                Width = 200,
            };
            _floyouts[category] = flyout;
            _stack[category] = menuItemsPanel;

            foreach (EditorToolbarButton item in category.Buttons)
            {
                CreateMenuButton(category, item);
            }

            flyout.Content = menuItemsPanel;
            FlyoutBase.SetAttachedFlyout(button, flyout);

            button.Click += (s, e) =>
            {
                FlyoutBase.ShowAttachedFlyout(button);
            };

            return button;
        }
    
        public Button CreateMenuButton(EditorToolbarCategory category, EditorToolbarButton button)
        {
            var menuItem = new Button
            {
                Content = button.Text,
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 6)
            };

            menuItem.Click += (s, e) =>
            {
                button?.Action?.Invoke();
                _floyouts[category].Hide();
            };

            _stack[category].Children.Add(menuItem);
            return menuItem;
        }
    }

}