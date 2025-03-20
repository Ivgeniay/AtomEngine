using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using System.Linq;
using Avalonia;
using System;
using Avalonia.Threading;

namespace Editor
{
    internal class WorldController : Grid, IWindowed, ICacheble
    {
        public Action<object> OnClose { get; set; }

        private string _baseName = "world";
        private ListBox _worldsList;
        private ObservableCollection<string> _worlds;
        private ContextMenu _worldListContextMenu;
        private ContextMenu _worldContextMenu;

        public event EventHandler<string> WorldSelected;
        public event EventHandler<string> WorldCreated;
        public event EventHandler<(string, string)> WorldRenamed;
        public event EventHandler<string> WorldDeleted;

        private SceneManager _sceneManager;
        private bool _isOpen = false;

        public WorldController()
        {
            _worlds = new ObservableCollection<string>();

            InitializeUI();
            _worldContextMenu = CreateElementContextMenu();
            _worldListContextMenu = CreateListContextMenus();

            _sceneManager = ServiceHub.Get<SceneManager>();

            _sceneManager.OnSceneInitialize += UpdateWorlds;
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
                Content = "+",
                Classes = { "worldToolButton" },
                Command = new Command(() => CreateNewWorld(GetUniqueName()))
            };

            Button removeBtn = new Button
            {
                Content = "-",
                Classes = { "worldToolButton" },
                Command = new Command(() => RemoveWorld(_worldsList.SelectedItem as string))
            };


            btnHolder.Children.Add(plusBtn);
            btnHolder.Children.Add(removeBtn);

            _worldsList = new ListBox
            {
                Classes = { "worldsList" },
            };
            _worldsList.ItemsSource = _worlds;
            _worldsList.AutoScrollToSelectedItem = true;
            _worldsList.SelectionMode = SelectionMode.Single;
            _worldsList.ItemsPanel = new FuncTemplate<Panel>(() =>
            {
                return new StackPanel
                {
                    Classes = { "worldCell" },
                    Orientation = Orientation.Vertical
                };
            });
            _worldsList.ItemTemplate = new FuncDataTemplate<string>((worldName, scope) =>
            {
                var entityName = new TextBlock
                {
                    Classes = { "worldName" },
                };
                entityName.Text = worldName;
                return entityName;
            });
            ScrollViewer.SetHorizontalScrollBarVisibility(_worldsList, ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(_worldsList, ScrollBarVisibility.Auto);

            _worldsList.SelectionChanged += (s, e) =>
            {
                if (_worldsList.SelectedItem is string selectedWorld)
                {
                    WorldSelected?.Invoke(this, selectedWorld);
                }
            };
            _worldsList.PointerReleased += (s, e) =>
            {
                var point = e.GetCurrentPoint(_worldsList);
                if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                {
                    _worldListContextMenu.Open(this);
                    e.Handled = true;
                }
                else
                {
                    _worldListContextMenu.Close();
                }
            };
            _worldsList.AddHandler(InputElement.PointerReleasedEvent, (s, e) =>
            {
                var visual = e.Source as Visual;
                if (visual != null)
                {
                    var item = visual.DataContext as string;
                    if (item != null && e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                    {
                        if (_worldsList.ItemsSource is ObservableCollection<string> collection)
                        {
                            int index = collection.IndexOf(item);
                            if (index > -1)
                            {
                                _worldsList.Selection.Clear();
                                _worldsList.Selection.Select(index);
                                WorldSelected?.Invoke(this, item);
                                _worldContextMenu.Open(this);
                                e.Handled = true;
                            }
                            else
                            {
                                _worldContextMenu.Close();
                            }
                        }
                    }
                }
            }, RoutingStrategies.Tunnel);

            Grid.SetRow(_worldsList, 1);

            Children.Add(btnHolder);
            Children.Add(_worldsList);
        }

        public void CreateNewWorld(string name, bool withInvoking = true)
        {
            _worlds.Add(name);
            if (withInvoking) WorldCreated?.Invoke(this, name);
            //_worldsList.SelectedItem = name;
        }

        private ContextMenu CreateListContextMenus()
        {
            var _worldListContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var createWorldItem = new MenuItem
            {
                Header = "Create New World",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(() => CreateNewWorld(GetUniqueName()))
            };

            _worldListContextMenu.Items.Add(createWorldItem);
            return _worldListContextMenu;
        }

        private ContextMenu CreateElementContextMenu()
        {
            var _worldContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var rename = new MenuItem()
            {
                Header = "Rename",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(() =>
                {
                    if (_worldsList.SelectedItem is string selectedEntity)
                    {
                        StartRenaming(selectedEntity);
                    }
                })
            };

            var delete = new MenuItem
            {
                Header = "Delete",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(() => RemoveWorld(_worldsList.SelectedItem as string))
            };

            _worldContextMenu.Items.Add(rename);
            _worldContextMenu.Items.Add(delete);

            return _worldContextMenu;
        }

        private void StartRenaming(string worldName)
        {
            // Создаем текстовое поле для редактирования
            var textBox = new TextBox
            {
                Text = worldName,
                Width = 200,
                SelectionStart = 0,
                SelectionEnd = worldName.Length,
                Classes = { "renameTextBox" }
            };

            // Создаем Popup для отображения поля ввода
            var popup = new Popup
            {
                Child = textBox,
                Placement = PlacementMode.Pointer,
                IsOpen = true
            };

            // Добавляем Popup в визуальное дерево
            this.Children.Add(popup);

            textBox.Focus();

            // Обработчик завершения редактирования
            textBox.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        var newWorldName = textBox.Text;
                        WorldRenamed?.Invoke(this, (worldName, newWorldName));

                        if (_worldsList.ItemsSource is ObservableCollection<string> collection)
                        {
                            var index = collection.IndexOf(worldName);
                            if (index != -1)
                            {
                                collection.RemoveAt(index);
                                collection.Insert(index, newWorldName);
                            }
                        }
                    }
                    popup.IsOpen = false;
                    this.Children.Remove(popup);
                }
                else if (e.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    this.Children.Remove(popup);
                }
            };

            // Закрываем при потере фокуса
            textBox.LostFocus += (s, e) => {
                popup.IsOpen = false;
                this.Children.Remove(popup);
            };
        }
        
        private void RemoveWorld(string worldName)
        {
            _worlds.Remove(worldName);
            WorldDeleted?.Invoke(this, worldName);
        }
        
        private string GetUniqueName()
        {
            string name = _baseName;
            int counter = 1;

            while (_worlds.Any(e => e == name))
            {
                name = $"{_baseName} ({counter})";
                counter++;
            }

            return name;
        }
        
        private void CloseAllContextMenus()
        {
            _worldContextMenu?.Close();
            _worldListContextMenu?.Close();
        }

        private void CreateWorldsFromScene(ProjectScene _scene, bool withInvoking = true)
        {
            foreach (var item in _scene.Worlds)
            {
                CreateNewWorld(item.WorldName, withInvoking);
            }
        }

        public void Dispose()
        {
        }

        internal void ClearWorlds()
        {
            _worlds.Clear();
        }

        internal void UpdateWorlds(ProjectScene currentScene)
        {
            Redraw();
        }

        public void Open()
        {
            _isOpen = true;
            Redraw();
        }

        public void Close()
        {
            _isOpen = false;
        }

        public void Redraw()
        {
            ClearWorlds();
            if (_isOpen && _sceneManager.CurrentScene != null)
            {
                CreateWorldsFromScene(_sceneManager.CurrentScene, withInvoking: false);
            }
        }

        public void FreeCache()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                ClearWorlds();
            }));
        }
    }
}
