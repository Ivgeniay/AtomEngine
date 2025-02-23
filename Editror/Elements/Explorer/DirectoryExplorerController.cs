using System.Collections.ObjectModel;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using System.Linq;
using System.IO;
using System;

namespace Editor
{
    public class DirectoryExplorerController : Grid
    {
        private readonly string _rootPath;
        private string _currentPath;

        private readonly TreeView _treeView;
        private readonly ListBox _fileList;
        private readonly TextBlock _pathDisplay;
        private readonly Button _backButton;

        private readonly ObservableCollection<TreeViewItem> _treeItems;
        private readonly ObservableCollection<string> _fileItems;
        private readonly HashSet<string> _expandedPaths;

        private ContextMenu _explorerContextMenu;
        private ContextMenu _fileContextMenu;

        public event Action<string> FileSelected;
        public event Action<string> DirectorySelected;

        public DirectoryExplorerController()
        {
            Classes.Add("directoryExplorer");

            _rootPath = DirectoryExplorer.GetPath(DirectoryType.Assets);
            _currentPath = _rootPath;

            _treeItems = new ObservableCollection<TreeViewItem>();
            _fileItems = new ObservableCollection<string>();
            _expandedPaths = new HashSet<string>();

            _explorerContextMenu = CreateDirectoryContextMenu();
            _fileContextMenu = CreateFileContextMenu();

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

            RefreshView();
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
                var files = Directory.GetFiles(_currentPath)
                                   .Select(Path.GetFileName)
                                   .ToList();
                foreach (var file in files)
                {
                    _fileItems.Add(file);
                }
            }
            catch (UnauthorizedAccessError)
            { }
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

        private void OnFileListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0 && e.AddedItems[0] is string fileName)
            {
                string fullPath = Path.Combine(_currentPath, fileName);
                FileSelected?.Invoke(fullPath);
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
                        Command = new Command(OnCutCommand)
                    },
                    new MenuItem
                    {
                        Header = "Copy",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnCopyCommand)
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
                        Command = new Command(OnDeleteCommand)
                    },
                    new MenuItem
                    {
                        Header = "Rename",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnRenameCommand)
                    },
                }
            };

            return menu;
        }

        private ContextMenu CreateFileContextMenu()
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
                        Command = new Command(OnCutCommand)
                    },
                    new MenuItem
                    {
                        Header = "Copy",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnCopyCommand)
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
                        Command = new Command(OnDeleteCommand)
                    },
                    new MenuItem
                    {
                        Header = "Rename",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(OnRenameCommand)
                    },
                }
            };

            return menu;
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

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                _fileContextMenu.PlacementTarget = _fileList;
                _fileContextMenu?.Open(this);
                e.Handled = true;
            }
            else
            {
                _fileContextMenu.Close();
            }
        }

        #region Commands

        private string _clipboardPath;
        private bool _isCut;

        private void OnNewFolderCommand()
        {
            string basePath = _currentPath;
            string newFolderName = "New Folder";
            string folderPath = Path.Combine(basePath, newFolderName);

            // Находим уникальное имя
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

        private void OnCutCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                _clipboardPath = treeItem.Tag as string;
                _isCut = true;
            }
            else if (_fileList.SelectedItem is string fileName)
            {
                _clipboardPath = Path.Combine(_currentPath, fileName);
                _isCut = true;
            }
        }

        private void OnCopyCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                _clipboardPath = treeItem.Tag as string;
                _isCut = false;
            }
            else if (_fileList.SelectedItem is string fileName)
            {
                _clipboardPath = Path.Combine(_currentPath, fileName);
                _isCut = false;
            }
        }

        private void OnPasteCommand()
        {
            if (string.IsNullOrEmpty(_clipboardPath) || !File.Exists(_clipboardPath) && !Directory.Exists(_clipboardPath))
                return;

            string fileName = Path.GetFileName(_clipboardPath);
            string destPath = Path.Combine(_currentPath, fileName);

            // Проверяем, существует ли файл/папка с таким именем
            if (File.Exists(destPath) || Directory.Exists(destPath))
            {
                int counter = 1;
                string newName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);

                while (File.Exists(destPath) || Directory.Exists(destPath))
                {
                    destPath = Path.Combine(_currentPath, $"{newName} ({counter}){extension}");
                    counter++;
                }
            }

            try
            {
                if (Directory.Exists(_clipboardPath))
                {
                    if (_isCut)
                        Directory.Move(_clipboardPath, destPath);
                    else
                        DirectoryCopy(_clipboardPath, destPath, true);
                }
                else if (File.Exists(_clipboardPath))
                {
                    if (_isCut)
                        File.Move(_clipboardPath, destPath);
                    else
                        File.Copy(_clipboardPath, destPath);
                }

                if (_isCut)
                    _clipboardPath = null;

                RefreshView();
            }
            catch (Exception ex)
            {
                // Здесь можно добавить обработку ошибок
            }
        }

        private void OnDeleteCommand()
        {
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                string path = treeItem.Tag as string;
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            else if (_fileList.SelectedItem is string fileName)
            {
                string path = Path.Combine(_currentPath, fileName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            RefreshView();
        }

        private void OnRenameCommand()
        {
            // TODO: Добавить TextBox для переименования
            // Пока простая реализация
            if (_treeView.SelectedItem is TreeViewItem treeItem)
            {
                string oldPath = treeItem.Tag as string;
                string newPath = Path.Combine(Path.GetDirectoryName(oldPath), "Renamed Folder");
                if (Directory.Exists(oldPath))
                    Directory.Move(oldPath, newPath);
            }
            else if (_fileList.SelectedItem is string fileName)
            {
                string oldPath = Path.Combine(_currentPath, fileName);
                string newPath = Path.Combine(_currentPath, "Renamed " + fileName);
                if (File.Exists(oldPath))
                    File.Move(oldPath, newPath);
            }
            RefreshView();
        }

        private void OnOpenCommand()
        {
            if (_fileList.SelectedItem is string fileName)
            {
                string filePath = Path.Combine(_currentPath, fileName);
                if (File.Exists(filePath))
                {
                    // Здесь можно добавить логику открытия файла
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
    }
}