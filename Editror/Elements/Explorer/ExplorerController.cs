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
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives;


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

        private readonly ExpandableFileManager _expandableFileManager;
        private readonly ExplorerExpandableFileView _expandableFileView;
        public string CurrentPath
        {
            get
            {
                return _currentPath ?? _rootPath ?? string.Empty;
            }
        }

        private FileSystemWatcher fileSystem;
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

            _customContextMenus = new List<DescriptionCustomContextMenu>();
            _explorerContextMenu = CreateDirectoryContextMenu();
            _fileContextMenu = CreateDefaultFileContextMenu();

            // Инициализация менеджера раскрываемых файлов
            _expandableFileManager = new ExpandableFileManager();
            _expandableFileManager.StateChanged += OnExpandableFilesStateChanged;

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
            RefreshView();
            _expandableFileView = new ExplorerExpandableFileView(this, _expandableFileManager, _fileList);
            t = new ModelExpandableHandler(_expandableFileManager);
        }
        ModelExpandableHandler t;

        private void OnExpandableFilesStateChanged()
        {
            RefreshView();
        }


        public void RegisterCustomContextMenu(DescriptionCustomContextMenu description)
        {
            if (!_customContextMenus.Contains(description))
                _customContextMenus.Add(description);
        }

        public void RegisterExpandableFileHandler(ExpandableFileItem handler)
        {
            _expandableFileManager.RegisterHandler(handler);
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
                                           foreach (var ext in configs.ExcludeExtension)
                                           {
                                               var r = e.EndsWith(ext);
                                               if (r) return false;
                                           }
                                           return true;
                                       });

                    foreach (var file in files)
                    {
                        _fileItems.Add(file);
                        string fullPath = Path.Combine(_currentPath, file);
                        if (_expandableFileManager.IsFileExpanded(fullPath))
                        {
                            foreach (var childMarker in _expandableFileView.GetChildItemMarkers(fullPath))
                            {
                                _fileItems.Add(childMarker);
                            }
                        }
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

        private ExpandableFileItemChild FindChildItem(List<ExpandableFileItemChild> items, string name, int level)
        {
            foreach (var item in items)
            {
                if (item.Name == name && item.Level == level)
                    return item;

                if (item.Children.Count > 0)
                {
                    var found = FindChildItem(item.Children, name, level);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }



        private void AddChildItemToList(ExpandableFileItemChild item)
        {
            // Добавляем специальный маркер для дочернего элемента
            // Формат: "__CHILD__[IndentLevel]__[ParentFile]__[ChildName]"
            string childMarker = $"__CHILD__{item.Level}__{Path.GetFileName(item.ParentFilePath)}__{item.Name}";
            _fileItems.Add(childMarker);

            if (item.IsExpanded && item.Children.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    AddChildItemToList(child);
                }
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

            // Проверяем, является ли элемент дочерним
            if (_expandableFileView.IsChildItemMarker(item, out var childInfo))
            {
                var childItem = _expandableFileManager.FindChildItem(childInfo.parentPath, childInfo.name, childInfo.level);
                if (childItem != null)
                {
                    if (e.InitialPressMouseButton == MouseButton.Right)
                    {
                        // Создаем контекстное меню для дочернего элемента
                        var contextMenu = new ContextMenu
                        {
                            Classes = { "explorerMenu" },
                            Placement = PlacementMode.Pointer,
                            Items =
                    {
                        new MenuItem
                        {
                            Header = $"Выбрать {childItem.Name}",
                            Classes = { "explorerMenuItem" },
                            Command = new Command(() =>
                            {
                                Status.SetStatus($"Выбран элемент {childItem.Name}");
                            })
                        }
                    }
                        };

                        contextMenu.PlacementTarget = _fileList;
                        contextMenu.Open(this);
                    }
                    else if (e.InitialPressMouseButton == MouseButton.Left)
                    {
                        _expandableFileView.HandleChildItemClick(childItem, e);
                    }

                    e.Handled = true;
                    return;
                }
            }

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
            if (fileSystem == null) fileSystem = ServiceHub.Get<FileSystemWatcher>();
            fileSystem.AssetChanged += Redraw;
            fileSystem.AssetCreated += Redraw;
            fileSystem.AssetDeleted += Redraw;
            fileSystem.AssetRenamed += Redraw;

            _isOpen = true;
        }

        public void Close()
        {
            if (fileSystem == null) fileSystem = ServiceHub.Get<FileSystemWatcher>();
            fileSystem.AssetChanged -= Redraw;
            fileSystem.AssetCreated -= Redraw;
            fileSystem.AssetDeleted -= Redraw;
            fileSystem.AssetRenamed -= Redraw;

            _isOpen = false;
        }

        public void Dispose()
        {
        }

        public void Redraw()
        {
            if (_isOpen)
            {
                RefreshView();
                _expandableFileManager.RefreshExpandedFiles();
            }
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
    }


    public class ModelExpandableHandler
    {
        private readonly List<string> _supportedExtensions = new List<string>();
        private readonly ExpandableFileManager _fileManager;

        /// <summary>
        /// Создает новый экземпляр обработчика 3D-моделей
        /// </summary>
        public ModelExpandableHandler(ExpandableFileManager fileManager)
        {
            _fileManager = fileManager;

            // Добавляем поддерживаемые расширения
            _supportedExtensions.Add(".obj");
            _supportedExtensions.Add(".fbx");
            _supportedExtensions.Add(".3ds");
            _supportedExtensions.Add(".blend");

            RegisterHandler();
        }

        /// <summary>
        /// Регистрирует обработчик в менеджере
        /// </summary>
        private void RegisterHandler()
        {
            _fileManager.RegisterHandlerByExtension(
                "3D Model Viewer",
                "Позволяет просматривать и использовать внутреннюю структуру 3D-моделей",
                _supportedExtensions,
                GetModelChildItems,
                HandleModelChildItemDrag
            );
        }

        /// <summary>
        /// Получает дочерние элементы модели
        /// </summary>
        private IEnumerable<ExpandableFileItemChild> GetModelChildItems(string filePath)
        {
            var metadataManager = ServiceHub.Get<MetadataManager>();
            var metadata = metadataManager.GetMetadata(filePath) as ModelMetadata;

            if (metadata == null || metadata.MeshesData.Count == 0)
                yield break;

            // Строим иерархию дочерних элементов
            var pathToItem = new Dictionary<string, ExpandableFileItemChild>();
            var rootItems = new List<ExpandableFileItemChild>();

            // Сначала создаем все элементы
            foreach (var meshData in metadata.MeshesData)
            {
                var path = meshData.MeshPath;
                var name = string.IsNullOrEmpty(meshData.MeshName) ?
                    $"Mesh_{meshData.Index}" : meshData.MeshName;

                var item = ExpandableFileManager.CreateChildItem(
                    filePath,
                    name,
                    meshData,
                    GetPathLevel(meshData.MeshPath),
                    child => GetDisplayNameForMesh(child)
                );

                pathToItem[path] = item;

                if (string.IsNullOrEmpty(path))
                {
                    rootItems.Add(item);
                }
            }

            // Затем строим иерархию
            foreach (var meshData in metadata.MeshesData)
            {
                if (string.IsNullOrEmpty(meshData.MeshPath))
                    continue;

                var parentPath = GetParentPath(meshData.MeshPath);
                if (!string.IsNullOrEmpty(parentPath) && pathToItem.TryGetValue(parentPath, out var parentItem))
                {
                    var item = pathToItem[meshData.MeshPath];
                    parentItem.Children.Add(item);
                }
                else if (!rootItems.Contains(pathToItem[meshData.MeshPath]))
                {
                    rootItems.Add(pathToItem[meshData.MeshPath]);
                }
            }

            // Возвращаем только корневые элементы
            foreach (var item in rootItems)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Получает отображаемое имя для меша
        /// </summary>
        private string GetDisplayNameForMesh(ExpandableFileItemChild child)
        {
            if (child.Data is NodeModelData meshData)
            {
                string name = !string.IsNullOrEmpty(meshData.MeshName) ?
                    meshData.MeshName : $"Mesh_{meshData.Index}";

                return name;
            }

            return child.Name;
        }

        /// <summary>
        /// Обрабатывает перетаскивание дочернего элемента модели
        /// </summary>
        private void HandleModelChildItemDrag(ExpandableFileItemChild child, DragDropEventArgs args)
        {
            if (child.Data is NodeModelData meshData)
            {
                // Обработка перетаскивания элемента модели
                Status.SetStatus($"Перетаскивание меша {meshData.MeshName} из модели {Path.GetFileName(args.FileFullPath)}");

                // Дополнительная логика для обработки перетаскивания
                // Например, создание сущности в сцене с этим мешем
            }
        }

        public void HandleModelDropData(string jsonData)
        {
            try
            {
                // Определяем анонимный тип для десериализации
                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                };

                // Десериализуем JSON в динамический объект
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonData, settings);

                // Извлекаем необходимые данные
                string fileName = data.FileName;
                string fileFullPath = data.FileFullPath;
                string childName = data.ChildItem.Name;

                // Получаем метаданные модели
                var metadataManager = ServiceHub.Get<MetadataManager>();
                var metadata = metadataManager.GetMetadata(fileFullPath) as ModelMetadata;

                if (metadata != null)
                {
                    // Ищем соответствующий меш по имени
                    var meshData = metadata.MeshesData.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.MeshName) && m.MeshName == childName);

                    if (meshData != null)
                    {
                        // Обрабатываем перетаскивание меша
                        Status.SetStatus($"Обработка перетаскивания меша {meshData.MeshName} из модели {fileName}");

                        // Дополнительная логика для обработки перетаскивания
                        // Например, создание сущности в сцене с этим мешем
                        // ServiceHub.Get<SceneManager>().CreateEntityWithModel(fileFullPath, meshData.MeshPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при обработке перетаскивания: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает уровень вложенности пути
        /// </summary>
        private int GetPathLevel(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            return path.Count(c => c == '/');
        }

        /// <summary>
        /// Получает родительский путь
        /// </summary>
        private string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int lastSlashIndex = path.LastIndexOf('/');
            if (lastSlashIndex < 0)
                return string.Empty;

            return path.Substring(0, lastSlashIndex);
        }
    }

    public class ExplorerExpandableFileView
    {
        private readonly ExplorerController _explorerController;
        private readonly ExpandableFileManager _fileManager;
        private readonly ListBox _fileList;

        private Point _childItemDragStartPoint;
        private bool _isChildItemDragInProgress;
        private ExpandableFileItemChild _draggedChildItem;
        private string _draggedChildParentFile;

        public ExplorerExpandableFileView(ExplorerController explorerController, ExpandableFileManager fileManager, ListBox fileList)
        {
            _explorerController = explorerController;
            _fileManager = fileManager;
            _fileList = fileList;

            Initialize();
        }

        private void Initialize()
        {
            _fileList.ItemTemplate = CreateFileItemDataTemplate();

            _fileList.AddHandler(InputElement.PointerPressedEvent, OnChildItemPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _fileList.AddHandler(InputElement.PointerMovedEvent, OnChildItemPointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            _fileList.AddHandler(InputElement.PointerReleasedEvent, OnChildItemPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }


        private FuncDataTemplate<string> CreateFileItemDataTemplate()
        {
            return new FuncDataTemplate<string>((fileName, scope) =>
            {
                if (IsChildItemMarker(fileName, out var childInfo))
                {
                    var childItem = _fileManager.FindChildItem(childInfo.parentPath, childInfo.name, childInfo.level);
                    if (childItem != null)
                    {
                        return CreateChildItemTemplate(childItem);
                    }
                }

                return CreateFileItemTemplate(fileName);
            });
        }

        private Control CreateFileItemTemplate(string fileName)
        {
            if (fileName == null || string.IsNullOrEmpty(_explorerController.CurrentPath))
            {
                return new TextBlock { Text = fileName ?? "Неизвестный файл" };
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            string fullPath = Path.Combine(_explorerController.CurrentPath, fileName);

            bool isExpandable = _fileManager.CanExpandFile(fullPath);
            bool isExpanded = _fileManager.IsFileExpanded(fullPath);

            if (isExpandable)
            {
                var expandButton = new ToggleButton
                {
                    Content = isExpanded ? "▼" : "►",
                    Width = 16,
                    Height = 16,
                    IsChecked = isExpanded,
                    Margin = new Thickness(2),
                    Classes = { "expandButton" }
                };

                expandButton.Click += (sender, e) =>
                {
                    if (expandButton.IsChecked == true)
                    {
                        _fileManager.ExpandFile(fullPath);
                    }
                    else
                    {
                        _fileManager.CollapseFile(fullPath);
                    }
                };

                Grid.SetColumn(expandButton, 0);
                grid.Children.Add(expandButton);
            }
            var textBlock = new TextBlock
            {
                Text = fileName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            return grid;
        }

        private Control CreateChildItemTemplate(ExpandableFileItemChild childItem)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20 * (childItem.Level + 1)) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var indent = new Border
            {
                Background = null
            };
            Grid.SetColumn(indent, 0);
            grid.Children.Add(indent);

            if (childItem.Children.Count > 0)
            {
                var expandButton = new ToggleButton
                {
                    Content = childItem.IsExpanded ? "▼" : "►",
                    Width = 16,
                    Height = 16,
                    IsChecked = childItem.IsExpanded,
                    Margin = new Thickness(2),
                    Classes = { "expandButton" }
                };

                expandButton.Click += (sender, e) =>
                {
                    _fileManager.ToggleChildItemExpansion(childItem);
                };

                Grid.SetColumn(expandButton, 1);
                grid.Children.Add(expandButton);
            }

            var textBlock = new TextBlock
            {
                Text = childItem.DisplayName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };
            Grid.SetColumn(textBlock, 2);
            grid.Children.Add(textBlock);

            return grid;
        }

        public bool IsChildItemMarker(string item, out (string parentPath, string name, int level) childInfo)
        {
            childInfo = default;
            if (item == null || string.IsNullOrEmpty(_explorerController.CurrentPath))
            {
                return false;
            }

            if (item.StartsWith("__CHILD__"))
            {
                var parts = item.Split("__");
                if (parts.Length >= 5)
                {
                    int level = int.Parse(parts[2]);
                    string parentFileName = parts[3];
                    string childName = string.Join("__", parts.Skip(4));

                    string parentFullPath = Path.Combine(_explorerController.CurrentPath, parentFileName);

                    childInfo = (parentFullPath, childName, level);
                    return true;
                }
            }

            return false;
        }

        public string CreateChildItemMarker(ExpandableFileItemChild item)
        {
            if (item == null)
                return null;

            if (string.IsNullOrEmpty(item.ParentFilePath))
                return null;

            if (string.IsNullOrEmpty(item.Name))
                return null;

            return $"__CHILD__{item.Level}__{Path.GetFileName(item.ParentFilePath)}__{item.Name}";
        }

        public IEnumerable<string> GetChildItemMarkers(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                yield break;

            if (!File.Exists(filePath))
                yield break;

            if (!_fileManager.IsFileExpanded(filePath))
                yield break;

            var childItems = _fileManager.GetChildItems(filePath);

            if (childItems == null)
                yield break;

            foreach (var item in GetChildItemMarkersRecursive(childItems))
            {
                yield return item;
            }
        }

        private IEnumerable<string> GetChildItemMarkersRecursive(IEnumerable<ExpandableFileItemChild> items)
        {
            if (items == null)
                yield break;

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                string marker = CreateChildItemMarker(item);
                if (!string.IsNullOrEmpty(marker))
                {
                    yield return marker;
                }

                if (item.IsExpanded && item.Children != null && item.Children.Count > 0)
                {
                    foreach (var childMarker in GetChildItemMarkersRecursive(item.Children))
                    {
                        if (!string.IsNullOrEmpty(childMarker))
                        {
                            yield return childMarker;
                        }
                    }
                }
            }
        }

        private void OnChildItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(_fileList);
                if (e.Source is Visual visual && visual.DataContext is string item)
                {
                    if (IsChildItemMarker(item, out var childInfo))
                    {
                        var childItem = _fileManager.FindChildItem(childInfo.parentPath, childInfo.name, childInfo.level);
                        if (childItem != null)
                        {
                            _childItemDragStartPoint = e.GetPosition(null);
                            _isChildItemDragInProgress = false;
                            _draggedChildItem = childItem;
                            _draggedChildParentFile = childItem.ParentFilePath;
                        }
                    }
                }
            }
        }

        private void OnChildItemPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedChildItem != null && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                var currentPosition = e.GetPosition(null);

                if (!_isChildItemDragInProgress &&
                    (Math.Abs(currentPosition.X - _childItemDragStartPoint.X) > 3 ||
                     Math.Abs(currentPosition.Y - _childItemDragStartPoint.Y) > 3))
                {
                    _isChildItemDragInProgress = true;
                    StartChildItemDrag(e);
                }
            }
        }

        private void OnChildItemPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _draggedChildItem = null;
            _draggedChildParentFile = null;
            _isChildItemDragInProgress = false;
        }

        private void StartChildItemDrag(PointerEventArgs e)
        {
            var handler = _fileManager.GetExpandableHandler(_draggedChildParentFile);
            if (handler != null && _draggedChildItem != null)
            {
                var dragEvent = new DragDropEventArgs
                {
                    FilePath = Path.GetDirectoryName(_draggedChildParentFile),
                    FileName = Path.GetFileName(_draggedChildParentFile),
                    FileExtension = Path.GetExtension(_draggedChildParentFile),
                    FileFullPath = _draggedChildParentFile,
                    ChildItem = _draggedChildItem
                };

                try
                {
                    // Создаем копию дочернего элемента без функций для сериализации
                    var childItemForSerialization = new
                    {
                        ParentFilePath = _draggedChildItem.ParentFilePath,
                        Name = _draggedChildItem.Name,
                        Data = _draggedChildItem.Data,
                        Level = _draggedChildItem.Level,
                        IsExpanded = _draggedChildItem.IsExpanded,
                        DisplayName = _draggedChildItem.DisplayName
                        // Не включаем GetDisplayName и Children для предотвращения циклических ссылок
                    };

                    var eventForSerialization = new
                    {
                        FilePath = dragEvent.FilePath,
                        FileName = dragEvent.FileName,
                        FileExtension = dragEvent.FileExtension,
                        FileFullPath = dragEvent.FileFullPath,
                        ChildItem = childItemForSerialization
                    };

                    // Создаем настройки сериализации для Newtonsoft.Json
                    var settings = new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        Error = (sender, errorArgs) =>
                        {
                            errorArgs.ErrorContext.Handled = true;
                        }
                    };

                    // Сериализуем данные для перетаскивания
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(eventForSerialization, settings);

                    // Создаем объект данных для перетаскивания
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Text, jsonData);

                    // Начинаем операцию перетаскивания
                    DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Copy);
                    //{"FilePath":"D:\\Programming\\CS\\AtomEngine\\Editror\\bin\\Debug\\net9.0\\Assets\\Models","FileName":"Can.obj","FileExtension":".obj","FileFullPath":"D:\\Programming\\CS\\AtomEngine\\Editror\\bin\\Debug\\net9.0\\Assets\\Models\\Can.obj","ChildItem":{"ParentFilePath":"D:\\Programming\\CS\\AtomEngine\\Editror\\bin\\Debug\\net9.0\\Assets\\Models\\Can.obj","Name":"Can.obj","Data":{"Matrix":{"M11":1.0,"M12":0.0,"M13":0.0,"M14":0.0,"M21":0.0,"M22":1.0,"M23":0.0,"M24":0.0,"M31":0.0,"M32":0.0,"M33":1.0,"M34":0.0,"M41":0.0,"M42":0.0,"M43":0.0,"M44":1.0,"IsIdentity":true,"Translation":{"X":0.0,"Y":0.0,"Z":0.0}},"MeshName":"Can.obj","MeshPath":"Can.obj","Index":-1},"Level":0,"IsExpanded":false,"DisplayName":"Can.obj"}}

                    // Уведомляем обработчик о перетаскивании
                    handler.RaiseChildItemDragged(_draggedChildItem, dragEvent);
                }
                catch (Exception ex)
                {
                    // Обрабатываем исключение при сериализации
                    Status.SetStatus($"Ошибка при перетаскивании: {ex.Message}");
                }
            }
        }

        public void HandleChildItemClick(ExpandableFileItemChild childItem, PointerReleasedEventArgs e)
        {
            var point = e.GetPosition(e.Source as Visual);
            if (point.X < 30 && childItem.Children.Count > 0)
            {
                _fileManager.ToggleChildItemExpansion(childItem);
            }
            else
            {
                Status.SetStatus($"Выбран элемент {childItem.Name} из файла {Path.GetFileName(childItem.ParentFilePath)}");
            }
        }
    }

    public class ExpandableFileManager
    {
        private List<ExpandableFileItem> _expandableFileItems = new List<ExpandableFileItem>();
        private Dictionary<string, List<ExpandableFileItemChild>> _expandedFiles = new Dictionary<string, List<ExpandableFileItemChild>>();

        /// <summary>
        /// Событие изменения состояния раскрытых файлов
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// Регистрирует обработчик раскрываемых файлов
        /// </summary>
        public void RegisterHandler(ExpandableFileItem handler)
        {
            if (handler != null && !_expandableFileItems.Contains(handler))
                _expandableFileItems.Add(handler);
        }

        /// <summary>
        /// Регистрирует обработчик для раскрываемых файлов
        /// </summary>
        /// <param name="name">Название обработчика</param>
        /// <param name="description">Описание обработчика</param>
        /// <param name="canExpand">Функция для определения возможности раскрытия файла</param>
        /// <param name="getChildItems">Функция для получения дочерних элементов</param>
        /// <param name="onDrag">Обработчик события перетаскивания</param>
        /// <returns>Созданный обработчик</returns>
        public ExpandableFileItem RegisterHandler(
            string name,
            string description,
            Func<string, bool> canExpand,
            Func<string, IEnumerable<ExpandableFileItemChild>> getChildItems,
            Action<ExpandableFileItemChild, DragDropEventArgs> onDrag = null)
        {
            var handler = new ExpandableFileItem
            {
                Name = name,
                Description = description,
                CanExpand = canExpand,
                GetChildItems = getChildItems,
                OnChildItemDrag = onDrag
            };

            RegisterHandler(handler);
            return handler;
        }

        /// <summary>
        /// Регистрирует обработчик для раскрываемых файлов по расширению
        /// </summary>
        /// <param name="name">Название обработчика</param>
        /// <param name="description">Описание обработчика</param>
        /// <param name="extensions">Список поддерживаемых расширений</param>
        /// <param name="getChildItems">Функция для получения дочерних элементов</param>
        /// <param name="onDrag">Обработчик события перетаскивания</param>
        /// <returns>Созданный обработчик</returns>
        public ExpandableFileItem RegisterHandlerByExtension(
            string name,
            string description,
            IEnumerable<string> extensions,
            Func<string, IEnumerable<ExpandableFileItemChild>> getChildItems,
            Action<ExpandableFileItemChild, DragDropEventArgs> onDrag = null)
        {
            var extensionList = extensions.Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}").ToList();

            return RegisterHandler(
                name,
                description,
                path => File.Exists(path) && extensionList.Contains(Path.GetExtension(path).ToLowerInvariant()),
                getChildItems,
                onDrag
            );
        }

        /// <summary>
        /// Создает дочерний элемент для раскрываемого файла
        /// </summary>
        /// <param name="parentFilePath">Путь к родительскому файлу</param>
        /// <param name="name">Имя элемента</param>
        /// <param name="data">Данные элемента</param>
        /// <param name="level">Уровень вложенности</param>
        /// <param name="displayNameFunc">Функция для получения отображаемого имени</param>
        /// <returns>Созданный дочерний элемент</returns>
        public static ExpandableFileItemChild CreateChildItem(
            string parentFilePath,
            string name,
            object data = null,
            int level = 0,
            Func<ExpandableFileItemChild, string> displayNameFunc = null)
        {
            return new ExpandableFileItemChild
            {
                ParentFilePath = parentFilePath,
                Name = name,
                Data = data,
                Level = level,
                GetDisplayName = displayNameFunc ?? (child => child.Name)
            };
        }

        /// <summary>
        /// Проверяет, можно ли раскрыть файл
        /// </summary>
        public bool CanExpandFile(string filePath)
        {
            return GetExpandableHandler(filePath) != null;
        }

        /// <summary>
        /// Получает обработчик для файла
        /// </summary>
        public ExpandableFileItem GetExpandableHandler(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            foreach (var handler in _expandableFileItems)
            {
                if (handler.CanExpand(filePath))
                    return handler;
            }

            return null;
        }

        /// <summary>
        /// Проверяет, раскрыт ли файл
        /// </summary>
        public bool IsFileExpanded(string filePath)
        {
            return _expandedFiles.ContainsKey(filePath);
        }

        /// <summary>
        /// Раскрывает файл
        /// </summary>
        public bool ExpandFile(string filePath)
        {
            var handler = GetExpandableHandler(filePath);
            if (handler == null)
                return false;

            try
            {
                var childItems = handler.GetChildItems(filePath).ToList();
                if (childItems.Count > 0)
                {
                    _expandedFiles[filePath] = childItems;
                    StateChanged?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при раскрытии файла: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Сворачивает файл
        /// </summary>
        public bool CollapseFile(string filePath)
        {
            if (_expandedFiles.ContainsKey(filePath))
            {
                _expandedFiles.Remove(filePath);
                StateChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Получает дочерние элементы раскрытого файла
        /// </summary>
        public IEnumerable<ExpandableFileItemChild> GetChildItems(string filePath)
        {
            if (_expandedFiles.TryGetValue(filePath, out var children))
                return children;

            return Enumerable.Empty<ExpandableFileItemChild>();
        }

        /// <summary>
        /// Раскрывает или сворачивает дочерний элемент
        /// </summary>
        public void ToggleChildItemExpansion(ExpandableFileItemChild item)
        {
            if (item == null) return;

            item.IsExpanded = !item.IsExpanded;
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Ищет дочерний элемент по имени и уровню вложенности
        /// </summary>
        public ExpandableFileItemChild FindChildItem(string parentFilePath, string name, int level)
        {
            if (_expandedFiles.TryGetValue(parentFilePath, out var rootItems))
            {
                return FindChildItemRecursive(rootItems, name, level);
            }

            return null;
        }

        private ExpandableFileItemChild FindChildItemRecursive(List<ExpandableFileItemChild> items, string name, int level)
        {
            foreach (var item in items)
            {
                if (item.Name == name && item.Level == level)
                    return item;

                if (item.Children.Count > 0)
                {
                    var found = FindChildItemRecursive(item.Children, name, level);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Обновляет все раскрытые файлы
        /// </summary>
        public void RefreshExpandedFiles()
        {
            var filesToRefresh = _expandedFiles.Keys.ToList();
            bool needsUpdate = false;

            foreach (var filePath in filesToRefresh)
            {
                if (!File.Exists(filePath))
                {
                    _expandedFiles.Remove(filePath);
                    needsUpdate = true;
                    continue;
                }

                var handler = GetExpandableHandler(filePath);
                if (handler != null)
                {
                    try
                    {
                        var newItems = handler.GetChildItems(filePath).ToList();
                        _expandedFiles[filePath] = newItems;
                        needsUpdate = true;
                    }
                    catch (Exception ex)
                    {
                        Status.SetStatus($"Ошибка при обновлении файла {Path.GetFileName(filePath)}: {ex.Message}");
                        _expandedFiles.Remove(filePath);
                        needsUpdate = true;
                    }
                }
                else
                {
                    _expandedFiles.Remove(filePath);
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
                StateChanged?.Invoke();
        }
    }
    
    public class ExpandableFileItem
    {
        public event Action<ExpandableFileItemChild, DragDropEventArgs> ChildItemDragged;
        public Func<string, bool> CanExpand { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<string, IEnumerable<ExpandableFileItemChild>> GetChildItems { get; set; }
        public Action<ExpandableFileItemChild, DragDropEventArgs> OnChildItemDrag { get; set; }
        public void RaiseChildItemDragged(ExpandableFileItemChild child, DragDropEventArgs args)
        {
            ChildItemDragged?.Invoke(child, args);
            OnChildItemDrag?.Invoke(child, args);
        }
    }

    public class ExpandableFileItemChild
    {
        public string ParentFilePath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public object Data { get; set; }
        public Func<ExpandableFileItemChild, string> GetDisplayName { get; set; }
        public int Level { get; set; } = 0;
        public List<ExpandableFileItemChild> Children { get; set; } = new List<ExpandableFileItemChild>();
        public bool IsExpanded { get; set; } = false;
        public string DisplayName => GetDisplayName?.Invoke(this) ?? Name;
    }

    public class DragDropEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileFullPath { get; set; }
        public ExpandableFileItemChild ChildItem { get; set; }
    }
}