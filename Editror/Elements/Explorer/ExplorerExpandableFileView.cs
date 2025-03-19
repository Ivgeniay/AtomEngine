using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using System.Linq;
using System.IO;
using Avalonia;
using System;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives;


namespace Editor
{
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
                    Context = _draggedChildItem
                };

                try
                {
                    var childItemForSerialization = new
                    {
                        ParentFilePath = _draggedChildItem.ParentFilePath,
                        Name = _draggedChildItem.Name,
                        Data = _draggedChildItem.Data,
                        Level = _draggedChildItem.Level,
                        IsExpanded = _draggedChildItem.IsExpanded,
                        DisplayName = _draggedChildItem.DisplayName
                    };

                    var eventForSerialization = new
                    {
                        FilePath = dragEvent.FilePath,
                        FileName = dragEvent.FileName,
                        FileExtension = dragEvent.FileExtension,
                        FileFullPath = dragEvent.FileFullPath,
                        Context = childItemForSerialization
                    };

                    var settings = new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        Error = (sender, errorArgs) =>
                        {
                            errorArgs.ErrorContext.Handled = true;
                        }
                    };
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(eventForSerialization, settings);

                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Text, jsonData);

                    DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Copy);
                    handler.RaiseChildItemDragged(_draggedChildItem, dragEvent);
                }
                catch (Exception ex)
                {
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
}