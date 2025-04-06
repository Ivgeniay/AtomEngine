using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using AtomEngine;
using System.IO;
using Avalonia;
using System;
using EngineLib;


namespace Editor
{
    public class ExplorerController : Grid, IWindowed
    {
        public event Action<FileSelectionEvent> FileSelected;
        public event Action<string> DirectorySelected;

        private readonly string _rootPath;
        private string _currentPath;
        private readonly ExplorerConfigurations configs;

        private TreeView _treeView;
        private ListBox _fileList;
        private TextBlock _pathDisplay;
        private Button _backButton;
        private readonly Canvas _overlayCanvas;

        private readonly List<DescriptionFileCustomContextMenu> _customFileContextMenus;
        private readonly List<DescriptionFreeSpaceCustomContextMenu> _customFreeSpaceContextMenus;
        private readonly List<DescriptionDirectoryTreeCustomContextMenu> _customDirectoryContextMenus;

        private readonly ExpandableFileManager _expandableFileManager;
        private readonly ExplorerTreeView _treeViewController;
        private readonly ExplorerFileList _fileListController;
        private readonly ExplorerFileOperations _fileOperations;
        private readonly ExplorerDragDropHandler _dragDropHandler;
        private readonly ExplorerContextMenu _contextMenuManager;
        private readonly ExplorerExpandableFileView _expandableFileView;

        private readonly ModelExpandableHandler _modelExpandable;

        private FileSystemWatcher fileSystem;
        private SceneManager _sceneManager;

        private bool _isOpen = false;

        private PointerPressedEventArgs _lastPointerPressedEvent;
        private TreeViewItem _lastHoveredTreeItem;
        private ListBoxItem _dragItem;
        private Point _dragStartPoint;

        public string CurrentPath { get { return _currentPath ?? _rootPath ?? string.Empty; } }

        public ExplorerController()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.OnSceneDirty += (e) => Redraw();
            _sceneManager.OnSceneInitialize += (e) => Redraw();

            this.configs = ServiceHub
                .Get<Configuration>()
                .GetConfiguration<ExplorerConfigurations>(ConfigurationSource.ExplorerConfigs);

            Classes.Add("directoryExplorer");

            _rootPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
            _currentPath = _rootPath;

            _expandableFileManager = new ExpandableFileManager();
            _expandableFileManager.StateChanged += OnExpandableFilesStateChanged;

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var navigationPanel = CreateNavigationPanel();
            var splitPanel = CreateSplitPanel();

            Grid.SetRow(navigationPanel, 0);
            Grid.SetRow(splitPanel, 1);

            Children.Add(navigationPanel);
            Children.Add(splitPanel);

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

            _customFileContextMenus = new List<DescriptionFileCustomContextMenu>();
            _customFreeSpaceContextMenus = new List<DescriptionFreeSpaceCustomContextMenu>();
            _customDirectoryContextMenus = new List<DescriptionDirectoryTreeCustomContextMenu>();

            _contextMenuManager = new ExplorerContextMenu(
                                        this,
                                        _customFileContextMenus,
                                        _customFreeSpaceContextMenus,
                                        _customDirectoryContextMenus);

            _fileOperations = new ExplorerFileOperations(this, _overlayCanvas);
            _dragDropHandler = new ExplorerDragDropHandler(this, _fileList, _treeView, _overlayCanvas, _fileOperations);
            _expandableFileView = new ExplorerExpandableFileView(this, _expandableFileManager, _fileList);
            _fileListController = new ExplorerFileList(this, _fileList, configs, _expandableFileManager, _expandableFileView);
            _fileListController.FileSelected += OnFileSelected;
            _treeViewController = new ExplorerTreeView(this, _treeView, _rootPath);
            _treeViewController.DirectorySelected += OnDirectorySelected;

            _modelExpandable = new ModelExpandableHandler(_expandableFileManager);

            RefreshView();
        }

        private StackPanel CreateNavigationPanel()
        {
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

            return navigationPanel;
        }

        private Grid CreateSplitPanel()
        {
            var splitPanel = new Grid();
            splitPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            splitPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            splitPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var splitter = new GridSplitter();
            Grid.SetColumn(splitter, 1);

            _treeView = new TreeView
            {
                Classes = { "directoryTree" },
                Margin = new Avalonia.Thickness(5)
            };

            _fileList = new ListBox
            {
                Classes = { "fileList" },
                Margin = new Avalonia.Thickness(5)
            };

            Grid.SetColumn(_treeView, 0);
            Grid.SetColumn(_fileList, 2);

            splitPanel.Children.Add(_treeView);
            splitPanel.Children.Add(splitter);
            splitPanel.Children.Add(_fileList);

            return splitPanel;
        }

        private void OnExpandableFilesStateChanged()
        {
            RefreshView();
        }

        private void OnDirectorySelected(string path)
        {
            _currentPath = path;
            RefreshView();
            DirectorySelected?.Invoke(_currentPath);
        }

        private void OnFileSelected(FileSelectionEvent fileEvent)
        {
            FileSelected?.Invoke(fileEvent);
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

        public void RefreshView()
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
            _treeViewController.RefreshTreeView();
        }
        private void UpdateFileList()
        {
            if (!Directory.Exists(_currentPath))
            {
                var parent = Directory.GetParent(_currentPath);
                if (parent != null && parent.FullName.StartsWith(_rootPath))
                {
                    _currentPath = parent.FullName;
                }
            }

            _fileListController.UpdateFileList(_currentPath);
        }

        public void RegisterCustomContextMenu(DescriptionFileCustomContextMenu description)
        {
            _contextMenuManager.RegisterFileCustomContextMenu(description);
        }

        public void RegisterCustomContextMenu(DescriptionFreeSpaceCustomContextMenu description)
        {
            _contextMenuManager.RegisterFreeSpaceCustomContextMenu(description);
        }

        public void RegisterCustomContextMenu(DescriptionDirectoryTreeCustomContextMenu description)
        {
            _contextMenuManager.RegisterDirectoryCustomContextMenu(description);
        }

        public void RegisterExpandableFileHandler(ExpandableFileItem handler)
        {
            _expandableFileManager.RegisterHandler(handler);
        }

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

        public void ShowDirectoryContextMenu(Control target)
        {
            _contextMenuManager.ShowDirectoryContextMenu(target);
        }

        public void ShowFileContextMenu(string fileName, Control target)
        {
            _contextMenuManager.ShowFileContextMenu(fileName, target);
        }

        public void ShowChildItemContextMenu(ExpandableFileItemChild childItem, Control target)
        {
            _contextMenuManager.ShowChildItemContextMenu(childItem, target);
        }

        public void ShowEmptyAreaContextMenu(Control target)
        {
            _contextMenuManager.ShowEmptyAreaContextMenu(target);
        }

        public void ShowDropIndicator(Control target)
        {
            _dragDropHandler.ShowDropIndicator(target);
        }

        public void HideDropIndicator()
        {
            _dragDropHandler.HideDropIndicator();
        }

        public void HandleTreeViewDragOver(object sender, DragEventArgs e, TreeView treeView)
        {
            // Вызов делегирован в ExplorerDragDropHandler
            e.Handled = true;
        }

        public void HandleTreeViewDrop(object sender, DragEventArgs e, TreeView treeView)
        {
            // Вызов делегирован в ExplorerDragDropHandler
            e.Handled = true;
        }

        public void StartDragOperation(ListBoxItem dragItem, string fileName, PointerPressedEventArgs e)
        {
            _dragItem = dragItem;
            _dragStartPoint = e.GetPosition(null);
            _lastPointerPressedEvent = e;

            dragItem.AddHandler(InputElement.PointerMovedEvent, OnDragPointerMoved, RoutingStrategies.Tunnel);
            dragItem.AddHandler(InputElement.PointerReleasedEvent, OnDragPointerReleased, RoutingStrategies.Tunnel);
        }

        private void OnDragPointerMoved(object? sender, PointerEventArgs e)
        {
            _dragDropHandler.OnDragPointerMoved(sender, e);
        }
        private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _dragDropHandler.OnDragPointerReleased(sender, e);
        }

        #region Command Handlers

        public void OnNewFolderCommand() => _fileOperations.CreateNewFolder();
        public void OnNewFileCommand() => _fileOperations.CreateNewFile();
        public void OnCopyDirectoryCommand()
        {
            if (_treeViewController.SelectedItem is TreeViewItem treeItem)
            {
                string path = treeItem.Tag as string;
                if (path != null)
                {
                    _fileOperations.CopyDirectory(path);
                }
            }
        }

        public void OnCutDirectoryCommand()
        {
            if (_treeViewController.SelectedItem is TreeViewItem treeItem)
            {
                string path = treeItem.Tag as string;
                if (path != null)
                {
                    _fileOperations.CutDirectory(path);
                }
            }
        }

        public void OnDeleteDirectoryCommand()
        {
            if (_treeViewController.SelectedItem is TreeViewItem treeItem)
            {
                string path = treeItem.Tag as string;
                if (path != null)
                {
                    _fileOperations.DeleteDirectory(path);
                }
            }
        }

        public void OnRenameDirectoryCommand()
        {
            if (_treeViewController.SelectedItem is TreeViewItem treeItem)
            {
                _fileOperations.RenameDirectory(treeItem);
            }
        }
        
        public void OnCopyFileCommand()
        {
            if (_fileListController.SelectedItem is string fileName)
            {
                _fileOperations.CopyFile(fileName);
            }
        }

        public void OnCutFileCommand()
        {
            if (_fileListController.SelectedItem is string fileName)
            {
                _fileOperations.CutFile(fileName);
            }
        }

        public void OnDeleteFileCommand()
        {
            if (_fileListController.SelectedItem is string fileName)
            {
                _fileOperations.DeleteFile(fileName);
            }
        }

        public void OnRenameFileCommand()
        {
            if (_fileListController.SelectedItem is string fileName)
            {
                var fileListItem = _fileListController.GetListBoxItemByFileName(fileName);
                _fileOperations.RenameFile(fileName, fileListItem);
            }
        }

        public void OnOpenCommand()
        {
            if (_fileListController.SelectedItem is string fileName)
            {
                _fileOperations.OpenFile(fileName);
            }
        }

        public void OnPasteCommand()
        {
            _fileOperations.Paste();
        }

        #endregion

        #region IWindowed Implementation

        public Action<object> OnClose { get; set; }

        public void Open()
        {
            if (fileSystem == null) fileSystem = ServiceHub.Get<FileSystemWatcher>();
            fileSystem.FileChanged += Redraw;
            fileSystem.FileCreated += Redraw;
            fileSystem.FileDeleted += Redraw;
            fileSystem.FileRenamed += Redraw;

            _isOpen = true;
        }

        public void Close()
        {
            if (fileSystem == null) fileSystem = ServiceHub.Get<FileSystemWatcher>();
            fileSystem.FileChanged -= Redraw;
            fileSystem.FileCreated -= Redraw;
            fileSystem.FileDeleted -= Redraw;
            fileSystem.FileRenamed -= Redraw;

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

        #endregion
    }
}