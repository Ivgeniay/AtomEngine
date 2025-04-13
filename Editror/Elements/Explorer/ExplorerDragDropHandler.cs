using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System.Linq;
using System.IO;
using Avalonia;
using System;
using EngineLib;


namespace Editor
{
    public class ExplorerDragDropHandler
    {
        private readonly ExplorerController _controller;
        private readonly ListBox _fileList;
        private readonly Canvas _overlayCanvas;
        private readonly Border _dropIndicator;
        private readonly TreeView _treeView;
        private readonly ExplorerFileOperations _fileOperations;

        private ListBoxItem _dragItem;
        private Point _dragStartPoint;
        private bool _isDragInProgress = false;
        private PointerPressedEventArgs _lastPointerPressedEvent;
        private TreeViewItem _lastHoveredTreeItem;

        public ExplorerDragDropHandler(
            ExplorerController controller,
            ListBox fileList,
            TreeView treeView,
            Canvas overlayCanvas,
            ExplorerFileOperations fileOperations)
        {
            _controller = controller;
            _fileList = fileList;
            _treeView = treeView;
            _overlayCanvas = overlayCanvas;
            _fileOperations = fileOperations;
            _dropIndicator = CreateDropIndicator();

            Initialize();
        }

        private void Initialize()
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
        }

        private Border CreateDropIndicator()
        {
            var dropIndicator = new Border
            {
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.DodgerBlue),
                Background = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255)),
                IsVisible = false,
                IsHitTestVisible = false
            };

            _overlayCanvas.Children.Add(dropIndicator);

            return dropIndicator;
        }

        public void OnListBoxItemPointerPressed(object? sender, PointerPressedEventArgs e)
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

        public void OnDragPointerMoved(object? sender, PointerEventArgs e)
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

        public void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
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

            var fileEvent = new DragDropEventArgs()
            {
                FileName = fileName,
                FileFullPath = Path.Combine(_controller.CurrentPath, fileName),
                FileExtension = Path.GetExtension(fileName),
                FilePath = _controller.CurrentPath
            };

            var data = new DataObject();
            data.Set(DataFormats.Text, fileEvent.ToString());

            DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }

        private void OnTreeViewDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<DragDropEventArgs>(jsonData, GlobalDeserializationSettings.Settings);

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

        private void OnTreeViewDrop(object? sender, DragEventArgs e)
        {
            HideDropIndicator();

            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<DragDropEventArgs>(jsonData, GlobalDeserializationSettings.Settings);

                        if (_treeView.SelectedItem is TreeViewItem selectedItem &&
                            selectedItem.Tag is string targetPath &&
                            Directory.Exists(targetPath))
                        {
                            string destinationPath = Path.Combine(targetPath, fileEvent.FileName);

                            if (Path.GetDirectoryName(fileEvent.FileFullPath) != targetPath)
                            {
                                _fileOperations.HandleFileMoveOperation(fileEvent.FileFullPath, destinationPath);
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

        public void ShowDropIndicator(Control target)
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

        public void HideDropIndicator()
        {
            if (_dropIndicator != null)
            {
                _dropIndicator.IsVisible = false;
            }
        }
    }
}