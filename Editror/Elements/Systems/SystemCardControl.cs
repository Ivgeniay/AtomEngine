using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia;
using System;
using AtomEngine;

namespace Editor
{
    internal class SystemCardControl : Border
    {
        public event Action<SystemCardControl> OnContexMenuOpen;
        public event Action<SystemCardControl> OnContexMenuClosed;

        public event EventHandler Selected;
        public event EventHandler Delete;
        public event EventHandler MoveUp;
        public event EventHandler MoveDown;

        private SystemData _system;
        private bool _isSelected;
        private ContextMenu _contextMenu;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateSelectedState();
            }
        }

        public SystemData GetSystem() => _system;

        public SystemCardControl(SystemData system)
        {
            _system = system;

            Classes.Add("systemCard");

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = system.SystemFullTypeName,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 160,
                Classes = { "systemTitle" }
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 5
            };

            var upButton = new Button
            {
                Content = "▲",
                Classes = { "systemCardButton" }
            };

            var downButton = new Button
            {
                Content = "▼",
                Classes = { "systemCardButton" }
            };

            upButton.Click += (s, e) => MoveUp?.Invoke(this, EventArgs.Empty);
            downButton.Click += (s, e) => MoveDown?.Invoke(this, EventArgs.Empty);

            buttonsPanel.Children.Add(upButton);
            buttonsPanel.Children.Add(downButton);

            Grid.SetRow(titleBlock, 0);
            Grid.SetColumn(titleBlock, 0);
            Grid.SetColumnSpan(titleBlock, 2);

            Grid.SetRow(buttonsPanel, 1);
            Grid.SetColumn(buttonsPanel, 1);

            grid.Children.Add(titleBlock);
            grid.Children.Add(buttonsPanel);

            Child = grid;

            _contextMenu = CreateContextMenu();

            this.PointerPressed += OnPointerPressed;
        }

        private void UpdateSelectedState()
        {
            if (_isSelected)
            {
                this.Classes.Add("selected");
                this.BorderBrush = new SolidColorBrush(Color.Parse("#007ACC"));
                this.BorderThickness = new Thickness(2);
            }
            else
            {
                this.Classes.Remove("selected");
                this.BorderBrush = new SolidColorBrush(Color.Parse("#444444"));
                this.BorderThickness = new Thickness(1);
            }
        }

        private ContextMenu CreateContextMenu()
        {
            var contextMenu = new ContextMenu
            {
                Classes = { "systemContextMenu" }
            };

            var deleteMenuItem = new MenuItem
            {
                Header = "Delete",
                Classes = { "systemMenuItem" }
            };

            deleteMenuItem.Click += (s, args) => Delete?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(deleteMenuItem);
            return contextMenu;
        }

        private void OpenContexMenu()
        {
            _contextMenu.Open(this);
            OnContexMenuOpen?.Invoke(this);
        }

        private void CloseContexMenu()
        {
            _contextMenu.Close();
            OnContexMenuClosed.Invoke(this);
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                Selected?.Invoke(this, EventArgs.Empty);
                CloseContexMenu();
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _contextMenu.Open(this);
                OpenContexMenu();
                e.Handled = true;
            }
            else
            {
                CloseContexMenu();
            }
        }
    }
}