using AtomEngine;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Styling;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.ObjectModel;

namespace Editor
{
    internal class WorldController : Grid, IWindowed
    {
        private ListBox _worldsList;
        private ObservableCollection<string> _worlds;
        private ContextMenu _worldContextMenu;
        private Scene _scene;
        public Action<object> OnClose { get; set; }

        public event EventHandler<string> WorldSelected;
        public event EventHandler<string> WorldCreated;
        public event EventHandler<string> WorldRenamed;
        public event EventHandler<string> WorldDeleted;

        public WorldController(Scene scene)
        {
            _scene = scene;
            _worlds = new ObservableCollection<string>();

            InitializeUI();
            CreateContextMenus();
        }

        private void InitializeUI()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var btnHolder = new StackPanel
            {
                Classes = { "worldWindowHeader" },
            };
            Grid.SetRow(btnHolder, 0);

            Button plusBtn = new Button
            {
                Content = "ADD",
                Classes = { "toolButton" }
            };

            btnHolder.Children.Add(plusBtn);

            _worldsList = new ListBox
            {
                Classes = { "worldsList" },
            };
            _worldsList.ItemsSource = _worlds;
            _worldsList.AutoScrollToSelectedItem = true;
            _worldsList.SelectionMode = SelectionMode.Single;
            _worldsList.ItemsPanel = new FuncTemplate<Panel>(() => new StackPanel
            {
                Spacing = 12,
                Orientation = Orientation.Vertical
            });
            ScrollViewer.SetHorizontalScrollBarVisibility(_worldsList, ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(_worldsList, ScrollBarVisibility.Auto);

            _worldsList.SelectionChanged += (s, e) => {
                if (_worldsList.SelectedItem is string selectedWorld)
                {
                    WorldSelected?.Invoke(this, selectedWorld);
                }
            };
            _worldsList.PointerPressed += (s, e) =>
            {
                var point = e.GetCurrentPoint(_worldsList);
                if (point.Properties.IsRightButtonPressed)
                {
                    var item = FindItemByPosition(_worldsList, e.GetPosition(_worldsList));
                    if (item != null)
                    {
                        WorldSelected?.Invoke(this, item);
                        _worldContextMenu.Open(_worldsList);
                        e.Handled = true;
                    }
                }
            };
            Grid.SetRow(_worldsList, 1);

            Children.Add(btnHolder);
            Children.Add(_worldsList);

            CreateNewWorld("Kek");
            CreateNewWorld("Ko");
        }

        public void CreateNewWorld(string name)
        {
            _worlds.Add(name);
            _worldsList.SelectedItem = name;
            WorldCreated?.Invoke(this, name);
        }

        private string FindItemByPosition(ListBox listBox, Point point)
        {
            for (int i = 0; i < _worlds.Count; i++)
            {
                var container = listBox.ContainerFromIndex(i) as ListBoxItem;
                if (container != null)
                {
                    var bounds = container.Bounds;
                    if (bounds.Contains(point))
                    {
                        return _worlds[i];
                    }
                }
            }
            return default;
        }

        private void CreateContextMenus()
        {
            _worldContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var createEntityItem = new MenuItem
            {
                Header = "Create Entity",
                Classes = { "hierarchyMenuItem" }
            };
            //createEntityItem.Click += (s, e) => CreateNewEntity("New Entity");

            var createEmptyItem = new MenuItem
            {
                Header = "Create Empty",
                Classes = { "hierarchyMenuItem" }
            };

            var separatorItem = new MenuItem
            {
                Header = "-",
                Classes = { "hierarchySeparator" }
            };

            var transform3dItem = new MenuItem
            {
                Header = "3D Object",
                Classes = { "hierarchyMenuItem" }
            };

            ((ItemsControl)_worldContextMenu).Items.Add(createEntityItem);
            ((ItemsControl)_worldContextMenu).Items.Add(createEmptyItem);
            ((ItemsControl)_worldContextMenu).Items.Add(separatorItem);
            ((ItemsControl)_worldContextMenu).Items.Add(transform3dItem);
        }

        public void Dispose()
        {
        }
    }
}
