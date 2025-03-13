using System.Collections.ObjectModel;
using System.Collections.Generic;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using System.Linq;
using AtomEngine;
using System.IO;
using Avalonia;
using System;
using MouseButton = Avalonia.Input.MouseButton;
using Key = Avalonia.Input.Key;


namespace Editor
{
    public class ExplorerController : Grid, IWindowed
    {
        public event Action<FileSelectionEvent> FileSelected;
        public event Action<string> DirectorySelected;

        private readonly string _rootPath;
        private string _currentPath;
        private readonly ExplorerConfigurations configs;

        private readonly TreeView _treeView;
        private readonly ListBox _fileList;
        private readonly TextBlock _pathDisplay;
        private readonly Button _backButton;
        private readonly Canvas _overlayCanvas;

        private readonly ObservableCollection<TreeViewItem> _treeItems;
        private readonly ObservableCollection<string> _fileItems;
        private readonly HashSet<string> _expandedPaths;

        private List<DescriptionCustomContextMenu> _customContextMenus = new List<DescriptionCustomContextMenu>();
        private ContextMenu _explorerContextMenu;
        private ContextMenu _fileContextMenu;

        private AssetFileSystem fileSystem;
        private SceneManager _sceneManager;

        private bool _isOpen = false;

        public ExplorerController()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.OnSceneDirty += (e) => Redraw();
            _sceneManager.OnSceneInitialize += (e) => Redraw();

            this.configs = ServiceHub
                .Get<Configuration>()
                .GetConfiguration<ExplorerConfigurations>(ConfigurationSource.ExplorerConfigs);

            Classes.Add("directoryExplorer");

            _rootPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.Assets);
            _currentPath = _rootPath;

            _treeItems = new ObservableCollection<TreeViewItem>();
            _fileItems = new ObservableCollection<string>();
            _expandedPaths = new HashSet<string>();

            _explorerContextMenu = CreateDirectoryContextMenu();
            _fileContextMenu = CreateDefaultFileContextMenu();

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var navigationPanel = new StackPanel
            {
                Classes = { "navigationPanel" },
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Avalonia.Thickness(5)
            };

            _backButton = new Button
            {
                Classes = { "navButton" },
                Content = "←",
                Width = 30
            };
            _backButton.Click += OnBackButtonClick;

            _pathDisplay = new TextBlock
            {
                Classes = { "pathDisplay" },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(5, 0)
            };

            navigationPanel.Children.Add(_backButton);
            navigationPanel.Children.Add(_pathDisplay);

            var splitPanel = new Grid();
            splitPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            splitPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            splitPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var splitter = new GridSplitter();
            Grid.SetColumn(splitter, 1);

            _treeView = new TreeView
            {
                Classes = { "directoryTree" },
                Margin = new Avalonia.Thickness(5),
                ItemsSource = _treeItems,
            };

            _treeView.SelectionChanged += OnTreeViewSelectionChanged;

            _treeView.ContainerPrepared += (sender, e) =>
            {
                if (e.Container is TreeViewItem item)
                {
                    string path = item.Tag as string;
                    if (path != null)
                    {
                        bool shouldExpand = _expandedPaths.Contains(path) ||
                                          path == _currentPath ||
                                          _currentPath.StartsWith(path + Path.DirectorySeparatorChar);

                        if (shouldExpand)
                        {
                            item.IsExpanded = true;
                        }

                        item.PropertyChanged += (s, args) =>
                        {
                            if (args.Property.Name == "IsExpanded" && s is TreeViewItem tvi && tvi.Tag is string itemPath)
                            {
                                if (tvi.IsExpanded)
                                {
                                    _expandedPaths.Add(itemPath);

                                    var parent = Directory.GetParent(itemPath);
                                    while (parent != null && parent.FullName.StartsWith(_rootPath))
                                    {
                                        _expandedPaths.Add(parent.FullName);
                                        parent = parent.Parent;
                                    }
                                }
                                else
                                {
                                    _expandedPaths.Remove(itemPath);

                                    var pathsToRemove = _expandedPaths
                                        .Where(p => p.StartsWith(itemPath + Path.DirectorySeparatorChar))
                                        .ToList();

                                    foreach (var pathToRemove in pathsToRemove)
                                    {
                                        _expandedPaths.Remove(pathToRemove);
                                    }
                                }
                            }
                        };
                    }
                }
            };

            _fileList = new ListBox
            {
                Classes = { "fileList" },
                Margin = new Avalonia.Thickness(5),
                ItemsSource = _fileItems
            };
            

            _fileList.SelectionChanged += OnFileListSelectionChanged;

            Grid.SetColumn(_treeView, 0);
            Grid.SetColumn(_fileList, 2);

            splitPanel.Children.Add(_treeView);
            splitPanel.Children.Add(splitter);
            splitPanel.Children.Add(_fileList);

            Grid.SetRow(navigationPanel, 0);
            Grid.SetRow(splitPanel, 1);

            Children.Add(navigationPanel);
            Children.Add(splitPanel);

            _treeView.PointerReleased += OnTreeViewPointerReleased;
            _fileList.PointerReleased += OnFileListPointerReleased;

            _overlayCanvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = null,
                IsHitTestVisible = false
            };

            Grid.SetRow(_overlayCanvas, 1);
            Grid.SetColumnSpan(_overlayCanvas, 3);
            Children.Add(_overlayCanvas);

            InitializeDragAndDrop();
            //EnableTreeViewDragDrop();

            RefreshView();
        }

        public void RegisterCustomContextMenu(DescriptionCustomContextMenu description)
        {
            if (!_customContextMenus.Contains(description))
                _customContextMenus.Add(description);
        }

        private void RefreshView()
        {
            UpdatePathDisplay();
            UpdateTreeView();
            UpdateFileList();
            UpdateBackButton();
        }

        private void UpdatePathDisplay()
        {
            string relativePath = _currentPath.Replace(_rootPath, "Assets");
            _pathDisplay.Text = relativePath;
        }

        private void UpdateBackButton()
        {
            _backButton.IsEnabled = _currentPath != _rootPath;
        }

        private void UpdateTreeView()
        {
            _treeItems.Clear();
            var rootItem = CreateDirectoryTreeItem(new DirectoryInfo(_rootPath));
            _treeItems.Add(rootItem);

            ExpandTreeItems(rootItem);
        }

        private void ExpandTreeItems(TreeViewItem item)
        {
            if (item.Tag is string path)
            {
                bool shouldExpand = _expandedPaths.Contains(path) ||
                                  _expandedPaths.Any(ep => path.StartsWith(ep + Path.DirectorySeparatorChar)) ||
                                  path == _currentPath ||
                                  _currentPath.StartsWith(path + Path.DirectorySeparatorChar);

                if (shouldExpand)
                {
                    item.IsExpanded = true;
                    foreach (TreeViewItem child in item.Items)
                    {
                        ExpandTreeItems(child);
                    }
                }
            }
        }

        private TreeViewItem CreateDirectoryTreeItem(DirectoryInfo directoryInfo)
        {
            var item = new TreeViewItem
            {
                Header = directoryInfo.Name,
                Tag = directoryInfo.FullName
            };

            try
            {
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    item.Items.Add(CreateDirectoryTreeItem(dir));
                }
            }
            catch (UnauthorizedAccessError)
            { }

            return item;
        }

        private void UpdateFileList()
        {
            _fileItems.Clear();
            try
            {
                if (Directory.Exists(_currentPath))
                {
                    var files = Directory.GetFiles(_currentPath)
                                       .Select(Path.GetFileName)
                                       .Where(e =>
                                       {
                                           foreach(var ext in configs.ExcludeExtension)
                                           {
                                               var r = e.EndsWith(ext);
                                               if (r) return false;
                                           }
                                           return true;
                                       });

                    foreach (var file in files)
                    {
                        _fileItems.Add(file);
                    }
                }
                else
                {
                    var parent = Directory.GetParent(_currentPath);
                    if (parent != null && parent.FullName.StartsWith(_rootPath))
                    {
                        _currentPath = parent.FullName;
                        UpdateFileList();
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void OnBackButtonClick(object? sender, RoutedEventArgs e)
        {
            if (_currentPath == _rootPath) return;

            var parent = Directory.GetParent(_currentPath);
            if (parent != null && parent.FullName.StartsWith(_rootPath))
            {
                _currentPath = parent.FullName;
                RefreshView();
                DirectorySelected?.Invoke(_currentPath);
            }
        }

        private void OnTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0 && e.AddedItems[0] is TreeViewItem selected)
            {
                string path = selected.Tag as string;
                if (path != null && Directory.Exists(path))
                {
                    _currentPath = path;
                    RefreshView();
                    DirectorySelected?.Invoke(_currentPath);
                }
            }
        }

        private string _selectedFile = string.Empty;
        private void OnFileListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0 && e.AddedItems[0] is string fileName)
            {
                _selectedFile = fileName;
            }
        }

        private ContextMenu CreateDirectoryContextMenu()
        {
            var menu = new ContextMenu
            {
                Classes = { "explorerMenu" },
                Placement = PlacementMode.Pointer,
                Items =
                {
                    new MenuItem
                    {
                        Header = "New Folder",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnNewFolderCommand)
                    },
                    new MenuItem
                    {
                        Header = "New File",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnNewFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "-",
                        Classes = { "explorerMenuSeparator" }
                    },
                    new MenuItem
                    {
                        Header = "Cut",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnCutDirectoryCommand)
                    },
                    new MenuItem
                    {
                        Header = "Copy",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnCopyDirectoryCommand)
                    },
                    new MenuItem
                    {
                        Header = "Paste",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnPasteCommand)
                    },
                    new MenuItem
                    {
                        Header = "-",
                        Classes = { "explorerMenuSeparator" }
                    },
                    new MenuItem
                    {
                        Header = "Delete",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnDeleteDirectoryCommand)
                    },
                    new MenuItem
                    {
                        Header = "Rename",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnRenameDirectoryCommand)
                    },
                }
            };

            return menu;
        }

        private ContextMenu CreateDefaultFileContextMenu()
        {
            var menu = new ContextMenu
            {
                Classes = { "explorerMenu" },
                Placement = PlacementMode.Pointer,
                Items =
                {
                    new MenuItem
                    {
                        Header = "Open",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnOpenCommand)
                    },
                    new MenuItem
                    {
                        Header = "-",
                        Classes = { "explorerMenuSeparator" }
                    },
                    new MenuItem
                    {
                        Header = "Cut",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnCutFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "Copy",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnCopyFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "Paste",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnPasteCommand)
                    },
                    new MenuItem
                    {
                        Header = "-",
                        Classes = { "explorerMenuSeparator" }
                    },
                    new MenuItem
                    {
                        Header = "Delete",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnDeleteFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "Rename",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnRenameFileCommand)
                    },
                }
            };

            return menu;
        }

        private ContextMenu CreateFileContextMenu(string filename, string path)
        {
            var contextMenu = CreateDefaultFileContextMenu();

            string extension = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(extension))
                return contextMenu;

            var customMenus = _customContextMenus
                .Where(menu => menu.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (customMenus.Any())
            {
                contextMenu.Items.Insert(0, new MenuItem
                {
                    Header = "-",
                    Classes = { "explorerMenuSeparator" }
                });

                foreach (var customMenu in customMenus)
                {
                    if (customMenu.SubCategory != null && customMenu.SubCategory.Length > 0)
                    {
                        MenuItem Item = new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenu" , "explorerMenuItem" },
                            Command = new Command(() => customMenu.Action?.Invoke(new FileSelectionEvent
                            {
                                FileName = filename, 
                                FileFullPath = path,
                                FilePath = path.Substring(0, path.IndexOf(filename)),
                                FileExtension = extension
                            })),
                        };

                        MenuItem root = new MenuItem
                        {
                            Header = customMenu.SubCategory[0],
                            Classes = { "explorerMenuItem" },
                        };
                        contextMenu.Items.Insert(0, root);

                        var others = customMenu.SubCategory.Skip(1);
                        foreach (var subCategory in others)
                        {
                            var new_ = new MenuItem
                            {
                                Header = subCategory,
                                Classes = { "explorerMenu" , "explorerMenuItem" },
                            };


                            root.Items.Add(new_);
                            root = new_;
                            //new_.ContextMenu.Classes.Add("explorerMenu");
                        }

                        root.Items.Add(Item);
                    }
                    else
                    {
                        contextMenu.Items.Insert(0, new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenuItem" },
                            Command = new Command(() => customMenu.Action?.Invoke(new FileSelectionEvent
                            {
                                FileName = filename,
                                FileFullPath = path,
                                FilePath = path.Substring(0, path.IndexOf(filename)),
                                FileExtension = extension
                            })),
                        });
                    }
                }
            }

            return contextMenu;
        }

        private void OnTreeViewPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetPosition(_treeView);
            var item = _treeView.SelectedItem as TreeViewItem;

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                _explorerContextMenu.PlacementTarget = _treeView;
                _explorerContextMenu?.Open(this);
                e.Handled = true;
            }
            else
            {
                _explorerContextMenu.Close();
            }
        }

        private void OnFileListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetPosition(_fileList);
            var item = _fileList.SelectedItem as string;
            if (item == null) return;

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                ContextMenu contexMenu = null;
                bool isThereExtension = item.IndexOf(".") > -1;
                if (isThereExtension)
                {
                    string extension = Path.GetExtension(item);
                    if (_customContextMenus.Any(menu => menu.Extension == extension))
                    {
                        var _clipboardPath = Path.Combine(_currentPath, item);
                        contexMenu = CreateFileContextMenu(item, _clipboardPath);
                    }
                    else
                        contexMenu = _fileContextMenu;
                }
                else
                {
                    contexMenu = _fileContextMenu;
                }

                contexMenu.PlacementTarget = _fileList;
                contexMenu?.Open(this);
                e.Handled = true;
            }
            else if (e.InitialPressMouseButton == MouseButton.Left)
            {
                _fileContextMenu.Close();

                var visual = e.Source as Visual;
                if (visual != null)
                {
                    var clickedItem = visual.DataContext as string;

                    if (clickedItem != null && clickedItem == _selectedFile)
                    {
                        string fullPath = Path.Combine(_currentPath, _selectedFile);
                        FileSelected?.Invoke(new FileSelectionEvent()
                        {
                            FileName = _selectedFile,
                            FileFullPath = fullPath,
                            FileExtension = Path.GetExtension(fullPath)
                        });
                        _selectedFile = string.Empty;
                    }
                }
            }
            else
            {
                _fileContextMenu.Close();
            }
        }

        #region DrugNDrop

        private ListBoxItem _dragItem;
        private Point _dragStartPoint;
        private bool _isDragInProgress = false;
        private PointerPressedEventArgs _lastPointerPressedEvent;
        private Border _dropIndicator;
        private TreeViewItem _lastHoveredTreeItem;

        private void InitializeDragAndDrop()
        {
            _fileList.ContainerPrepared += (sender, e) =>
            {
                if (e.Container is ListBoxItem item)
                {
                    item.AddHandler(InputElement.PointerPressedEvent, OnListBoxItemPointerPressed, RoutingStrategies.Tunnel);
                }
            };

            DragDrop.SetAllowDrop(_treeView, true);
            _treeView.AddHandler(DragDrop.DragOverEvent, OnTreeViewDragOver);
            _treeView.AddHandler(DragDrop.DropEvent, OnTreeViewDrop);

            CreateDropIndicator();
        }

        private void CreateDropIndicator()
        {
            _dropIndicator = new Border
            {
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.DodgerBlue),
                Background = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255)),
                IsVisible = false,
                IsHitTestVisible = false
            };

            _overlayCanvas.Children.Add(_dropIndicator);
        }

        private void OnListBoxItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed &&
                sender is ListBoxItem item &&
                item.DataContext is string fileName)
            {
                if (_dragItem != null)
                {
                    _dragItem.RemoveHandler(InputElement.PointerMovedEvent, OnDragPointerMoved);
                    _dragItem.RemoveHandler(InputElement.PointerReleasedEvent, OnDragPointerReleased);
                    _dragItem = null;
                }

                _isDragInProgress = false;
                _dragItem = item;
                _dragStartPoint = e.GetPosition(null);
                _lastPointerPressedEvent = e;

                item.AddHandler(InputElement.PointerMovedEvent, OnDragPointerMoved, RoutingStrategies.Tunnel);
                item.AddHandler(InputElement.PointerReleasedEvent, OnDragPointerReleased, RoutingStrategies.Tunnel);
            }
        }

        private void OnDragPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragItem != null && _lastPointerPressedEvent != null && !_isDragInProgress)
            {
                var currentPosition = e.GetPosition(null);

                if (Math.Abs(currentPosition.X - _dragStartPoint.X) > 3 ||
                    Math.Abs(currentPosition.Y - _dragStartPoint.Y) > 3)
                {
                    if (_dragItem.DataContext is string fileName)
                    {
                        StartDragOperation(fileName, _lastPointerPressedEvent);
                    }
                }
            }
        }

        private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                item.RemoveHandler(InputElement.PointerMovedEvent, OnDragPointerMoved);
                item.RemoveHandler(InputElement.PointerReleasedEvent, OnDragPointerReleased);
            }

            _isDragInProgress = false;
            _dragItem = null;
            _lastPointerPressedEvent = null;
        }

        private void StartDragOperation(string fileName, PointerPressedEventArgs e)
        {
            _isDragInProgress = true;

            var fileEvent = new FileSelectionEvent()
            {
                FileName = fileName,
                FileFullPath = Path.Combine(_currentPath, fileName),
                FileExtension = Path.GetExtension(fileName),
                FilePath = _currentPath
            };

            var data = new DataObject();
            data.Set(DataFormats.Text, fileEvent.ToString());

            DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }



        // Directory
        private void OnTreeViewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(jsonData, GlobalDeserializationSettings.Settings);

                        var position = e.GetPosition(_treeView);
                        var treeItem = FindTreeViewItemAtPosition(_treeView, position);

                        if (treeItem != null && treeItem.Tag is string targetPath && Directory.Exists(targetPath))
                        {
                            if (Path.GetDirectoryName(fileEvent.FileFullPath) != targetPath)
                            {
                                ShowDropIndicator(treeItem);

                                e.DragEffects = DragDropEffects.Move;

                                if (_lastHoveredTreeItem != treeItem)
                                {
                                    _treeView.SelectedItem = treeItem;
                                    _lastHoveredTreeItem = treeItem;
                                }

                                e.Handled = true;
                                return;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            HideDropIndicator();
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnTreeViewDrop(object sender, DragEventArgs e)
        {
            HideDropIndicator();

            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(jsonData, GlobalDeserializationSettings.Settings);

                        // Получаем целевую папку
                        if (_treeView.SelectedItem is TreeViewItem selectedItem &&
                            selectedItem.Tag is string targetPath &&
                            Directory.Exists(targetPath))
                        {
                            // Формируем путь назначения
                            string destinationPath = Path.Combine(targetPath, fileEvent.FileName);

                            // Проверяем, не пытаемся ли переместить файл в ту же самую папку
                            if (Path.GetDirectoryName(fileEvent.FileFullPath) != targetPath)
                            {
                                HandleFileMoveOperation(fileEvent.FileFullPath, destinationPath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при перемещении файла: {ex.Message}");
                }
            }

            e.Handled = true;
        }

        private async void HandleFileMoveOperation(string sourcePath, string destinationPath)
        {
            try
            {
                bool overwrite = false;

                if (File.Exists(destinationPath))
                {
                    overwrite = await ShowFileExistsDialog(Path.GetFileName(destinationPath));

                    if (!overwrite)
                        return;
                }

                if (overwrite && File.Exists(destinationPath))
                    File.Delete(destinationPath);

                File.Move(sourcePath, destinationPath);

                RefreshView();
                Status.SetStatus($"Файл {Path.GetFileName(sourcePath)} перемещен успешно");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при перемещении файла: {ex.Message}");
            }
        }

        private TreeViewItem FindTreeViewItemAtPosition(TreeView treeView, Point position)
        {
            Visual visual = treeView.InputHitTest(position) as Visual;

            while (visual != null)
            {
                if (visual is TreeViewItem item)
                    return item;

                visual = visual.GetVisualAncestors().FirstOrDefault();
            }

            return null;
        }

        private void ShowDropIndicator(Control target)
        {
            if (_dropIndicator != null)
            {
                var bounds = target.Bounds;
                var position = target.TranslatePoint(new Point(0, 0), _overlayCanvas);

                if (position.HasValue)
                {
                    Canvas.SetLeft(_dropIndicator, position.Value.X);
                    Canvas.SetTop(_dropIndicator, position.Value.Y);
                    _dropIndicator.Width = bounds.Width;
                    _dropIndicator.Height = bounds.Height;
                    _dropIndicator.IsVisible = true;
                }
            }
        }

        private void HideDropIndicator()
        {
            if (_dropIndicator != null)
            {
                _dropIndicator.IsVisible = false;
            }
        }

        private async Task<bool> ShowFileExistsDialog(string fileName)
        {
            // Создаем диалоговое окно
            var window = new Window
            {
                Title = "Файл существует",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"Файл '{fileName}' уже существует. Перезаписать?",
                TextWrapping = TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            bool result = false;

            var yesButton = new Button { Content = "Да", Width = 80 };
            yesButton.Click += (s, e) =>
            {
                result = true;
                window.Close();
            };

            var noButton = new Button { Content = "Нет", Width = 80 };
            noButton.Click += (s, e) =>
            {
                result = false;
                window.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            panel.Children.Add(buttonPanel);
            window.Content = panel;

            // Отображаем диалог и ждем ответа
            await window.ShowDialog(GetWindowFromVisual(this));

            return result;
        }

        private Window GetWindowFromVisual(Control control)
        {
            while (control != null)
            {
                if (control is Window window)
                    return window;

                control = control.Parent as Control;
            }

            return null;
        }


        #endregion

        #region Commands

        private string _clipboardPath;
        private bool _isCut;
        private bool _isDirectory;

        public Action<object> OnClose => throw new NotImplementedException();

        private void OnNewFolderCommand()
        {
            string basePath = _currentPath;
            string newFolderName = "New Folder";
            string folderPath = Path.Combine(basePath, newFolderName);

            int counter = 1;
            while (Directory.Exists(folderPath))
            {
                folderPath = Path.Combine(basePath, $"{newFolderName} ({counter})");
                counter++;
            }

            Directory.CreateDirectory(folderPath);
            RefreshView();
        }

        private void OnNewFileCommand()
        {
            string basePath = _currentPath;
            string newFileName = "New File.txt";
            string filePath = Path.Combine(basePath, newFileName);

            // Находим уникальное имя
            int counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(basePath, $"New File ({counter}).txt");
                counter++;
            }

            File.Create(filePath).Close();
            RefreshView();
        }

        private void OnCopyFileCommand()
        {
            if (_fileList.SelectedItem is string fileName)
            {
                _clipboardPath = Path.Combine(_currentPath, fileName);
                _isCut = false;
                _isDirectory = false;
            }
        }

        private void OnCopyDirectoryCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                _clipboardPath = treeItem.Tag as string;
                _isCut = false;
                _isDirectory = true;
            }
        }

        private void OnCutFileCommand()
        {
            if (_fileList.SelectedItem is string fileName)
            {
                _clipboardPath = Path.Combine(_currentPath, fileName);
                _isCut = true;
                _isDirectory = false;
            }
        }

        private void OnCutDirectoryCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                _clipboardPath = treeItem.Tag as string;
                _isCut = true;
                _isDirectory = true;
            }
        }

        private void OnPasteCommand()
        {
            if (string.IsNullOrEmpty(_clipboardPath) || (!File.Exists(_clipboardPath) && !Directory.Exists(_clipboardPath)))
                return;

            try
            {
                string sourceName = Path.GetFileName(_clipboardPath);
                string destPath = Path.Combine(_currentPath, sourceName);

                if (File.Exists(destPath) || Directory.Exists(destPath))
                {
                    int counter = 1;
                    if (_isDirectory)
                    {
                        while (Directory.Exists(destPath))
                        {
                            destPath = Path.Combine(_currentPath, $"{sourceName} ({counter})");
                            counter++;
                        }
                    }
                    else
                    {
                        string name = Path.GetFileNameWithoutExtension(sourceName);
                        string ext = Path.GetExtension(sourceName);
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(_currentPath, $"{name} ({counter}){ext}");
                            counter++;
                        }
                    }
                }

                if (_isDirectory)
                {
                    if (_isCut)
                        Directory.Move(_clipboardPath, destPath);
                    else
                        DirectoryCopy(_clipboardPath, destPath, true);
                }
                else
                {
                    if (_isCut)
                        File.Move(_clipboardPath, destPath);
                    else
                        File.Copy(_clipboardPath, destPath);
                }

                if (_isCut)
                {
                    _clipboardPath = null;
                    _isDirectory = false;
                }

                RefreshView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during paste operation: {ex.Message}");
            }
        }

        private void OnDeleteFileCommand()
        {
            if (_fileList.SelectedItem is string fileName)
            {
                string path = Path.Combine(_currentPath, fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    RefreshView();
                }
            }
        }

        private void OnDeleteDirectoryCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                string path = treeItem.Tag as string;
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    RefreshView();
                }
            }
        }

        private void OnRenameFileCommand()
        {
            if (_fileList.SelectedItem is string fileName)
            {
                var selectedIndex = _fileList.SelectedIndex;
                var listBoxItem = _fileList.GetLogicalChildren()
                    .OfType<ListBoxItem>()
                    .FirstOrDefault(x => x.DataContext as string == fileName);

                if (listBoxItem == null) return;

                // Получаем позицию относительно нашего Grid
                var position = listBoxItem.TranslatePoint(new Point(0, 0), this);
                if (position == null) return;

                var textBox = new TextBox
                {
                    Text = fileName,
                    Classes = { "renameTextBox" },
                    Width = listBoxItem.Bounds.Width,
                    Height = listBoxItem.Bounds.Height
                };
                textBox.IsHitTestVisible = true;

                Canvas.SetLeft(textBox, position.Value.X);
                Canvas.SetTop(textBox, position.Value.Y);

                _overlayCanvas.Children.Add(textBox);

                void OnPointerPressed(object s, PointerPressedEventArgs e)
                {
                    var point = e.GetPosition(textBox);

                    if (point.X < 0 || point.X > textBox.Width ||
                        point.Y < 0 || point.Y > textBox.Height)
                    {
                        _overlayCanvas.Children.Remove(textBox);
                        this.PointerPressed -= OnPointerPressed;
                    }
                }

                this.PointerPressed += OnPointerPressed;

                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        this.PointerPressed -= OnPointerPressed;

                        string oldPath = Path.Combine(_currentPath, fileName);
                        string newPath = Path.Combine(_currentPath, textBox.Text);

                        if (File.Exists(oldPath) && !File.Exists(newPath))
                        {
                            try
                            {
                                File.Move(oldPath, newPath);
                                RefreshView();
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        _overlayCanvas.Children.Remove(textBox);
                    }
                    else if (e.Key == Key.Escape)
                    {
                        this.PointerPressed -= OnPointerPressed;
                        _overlayCanvas.Children.Remove(textBox);
                    }
                };

                textBox.LostFocus += (s, e) =>
                {
                    this.PointerPressed -= OnPointerPressed;
                    _overlayCanvas.Children.Remove(textBox);
                };

                textBox.Focus();
                textBox.SelectAll();
            }
        }

        private void OnRenameDirectoryCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem selectedItem)
            {
                string originalPath = selectedItem.Tag as string;
                if (originalPath == null) return;

                string originalName = Path.GetFileName(originalPath);

                var treeViewItem = FindTreeViewItemByPath(_treeView, originalPath);
                if (treeViewItem == null) return;

                var position = treeViewItem.TranslatePoint(new Point(0, 0), this);
                if (position == null) return;

                var textBox = new TextBox
                {
                    Text = originalName,
                    Classes = { "renameTextBox" },
                    Width = treeViewItem.Bounds.Width - 20,
                    Height = 20 
                };
                textBox.IsHitTestVisible = true;

                Canvas.SetLeft(textBox, position.Value.X + 20);
                Canvas.SetTop(textBox, position.Value.Y);

                _overlayCanvas.Children.Add(textBox);

                void OnPointerPressed(object s, PointerPressedEventArgs e)
                {
                    var point = e.GetPosition(textBox);
                    if (point.X < 0 || point.X > textBox.Width ||
                        point.Y < 0 || point.Y > textBox.Height)
                    {
                        _overlayCanvas.Children.Remove(textBox);
                        this.PointerPressed -= OnPointerPressed;
                    }
                }

                this.PointerPressed += OnPointerPressed;

                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        this.PointerPressed -= OnPointerPressed;
                        string parentPath = Path.GetDirectoryName(originalPath);
                        string newPath = Path.Combine(parentPath, textBox.Text);

                        if (Directory.Exists(originalPath) && !Directory.Exists(newPath))
                        {
                            try
                            {
                                Directory.Move(originalPath, newPath);
                                RefreshView();
                            }
                            catch (Exception ex)
                            { }
                        }
                        _overlayCanvas.Children.Remove(textBox);
                    }
                    else if (e.Key == Key.Escape)
                    {
                        this.PointerPressed -= OnPointerPressed;
                        _overlayCanvas.Children.Remove(textBox);
                    }
                };

                textBox.Focus();
                textBox.SelectAll();
            }
        }

        private TreeViewItem FindTreeViewItemByPath(TreeView treeView, string path)
        {
            foreach (var item in treeView.GetLogicalDescendants().OfType<TreeViewItem>())
            {
                if (item.Tag as string == path)
                {
                    return item;
                }
            }
            return null;
        }

        private void OnOpenCommand()
        {
            if (_fileList.SelectedItem is string fileName)
            {
                string filePath = Path.Combine(_currentPath, fileName);
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }
        }

        private static void DirectoryCopy(string sourcePath, string destPath, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourcePath);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destPath, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        #endregion
        
        public void ExcludeFileExtension(string e)
        {
            if (string.IsNullOrEmpty(e))
            {
                DebLogger.Error("Extensions should't be white space");
                return;
            }
            this.configs.ExcludeExtension.Add(e);
            ServiceHub.Get<Configuration>().SafeConfiguration(ConfigurationSource.ExplorerConfigs, this.configs);
            DebLogger.Info($"{e} was added as excluded file extension");
        }

        public void Open()
        {
            if (fileSystem == null) fileSystem = ServiceHub.Get<AssetFileSystem>();
            fileSystem.AssetChanged += Redraw;
            fileSystem.AssetCreated += Redraw;
            fileSystem.AssetDeleted += Redraw;
            fileSystem.AssetRenamed += Redraw;

            _isOpen = true;
        }

        public void Close()
        {
            if (fileSystem == null) fileSystem = ServiceHub.Get<AssetFileSystem>();
            fileSystem.AssetChanged -= Redraw;
            fileSystem.AssetCreated -= Redraw;
            fileSystem.AssetDeleted -= Redraw;
            fileSystem.AssetRenamed -= Redraw;

            _isOpen = false;
        }

        public void Dispose()
        {
        }

        private void Redraw(FileEvent fileChangedEvent)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Redraw();
            });
        }
        private void Redraw(string filePath)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Redraw();
            });
        }
        private void Redraw(string oldFilePath, string newFilePath)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Redraw();
            });
        }
        public void Redraw()
        {
            if (_isOpen)
            {
                RefreshView();
            }
        }
    }
}