﻿using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using System.IO;
using Avalonia;
using System;
using MouseButton = Avalonia.Input.MouseButton;
using Avalonia.VisualTree;
using EngineLib;


namespace Editor
{
    public class ExplorerFileList
    {
        private readonly ExplorerController _controller;
        private readonly ListBox _fileList;
        private readonly ObservableCollection<string> _fileItems;
        private readonly ExplorerConfigurations _configs;
        private readonly ExpandableFileManager _expandableFileManager;
        private readonly ExplorerExpandableFileView _expandableFileView;


        public event Action<FileSelectionEvent> FileSelected;

        private string _selectedFile = string.Empty;

        public ExplorerFileList(
            ExplorerController controller,
            ListBox fileList,
            ExplorerConfigurations configs,
            ExpandableFileManager expandableFileManager,
            ExplorerExpandableFileView expandableFileView)
        {
            _controller = controller;
            _fileList = fileList;
            _configs = configs;
            _expandableFileManager = expandableFileManager;
            _expandableFileView = expandableFileView;
            _fileItems = new ObservableCollection<string>();


            Initialize();
        }

        private void Initialize()
        {
            _fileList.ItemsSource = _fileItems;
            _fileList.SelectionChanged += OnFileListSelectionChanged;
            _fileList.PointerReleased += OnFileListPointerReleased;

            _fileList.ContainerPrepared += (sender, e) =>
            {
                if (e.Container is ListBoxItem item)
                {
                    item.AddHandler(InputElement.PointerPressedEvent, OnListBoxItemPointerPressed, RoutingStrategies.Tunnel);
                }
            };
        }

        public void UpdateFileList(string currentPath)
        {
            _fileItems.Clear();
            try
            {
                if (Directory.Exists(currentPath))
                {
                    var files = Directory.GetFiles(currentPath)
                                      .Select(Path.GetFileName)
                                      .Where(e =>
                                      {
                                          foreach (var ext in _configs.ExcludeExtension)
                                          {
                                              var r = e.EndsWith(ext);
                                              if (r) return false;
                                          }
                                          return true;
                                      });

                    foreach (var file in files)
                    {
                        _fileItems.Add(file);
                        string fullPath = Path.Combine(currentPath, file);
                        if (_expandableFileManager.IsFileExpanded(fullPath))
                        {
                            foreach (var childMarker in _expandableFileView.GetChildItemMarkers(fullPath))
                            {
                                _fileItems.Add(childMarker);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void OnFileListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0 && e.AddedItems[0] is string fileName)
            {
                _selectedFile = fileName;
            }
        }

        private void OnFileListPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
            {
                var item = _fileList.GetLogicalDescendants()
                    .OfType<ListBoxItem>()
                    .FirstOrDefault(x => x.IsPointerOver);

                if (item == null)
                {
                    //_controller.ShowDirectoryContextMenu(_fileList);
                    _controller.ShowEmptyAreaContextMenu(_fileList);
                    e.Handled = true;
                }
            }
        }

        private void OnFileListPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var visual = e.Source as Visual;
                var pointedItem = e.Source as ListBoxItem;

                if (pointedItem == null && visual != null)
                {
                    var ancestors = visual.GetVisualAncestors();
                    pointedItem = ancestors.OfType<ListBoxItem>().FirstOrDefault();
                }

                var selectedItem = _fileList.SelectedItem as string;

                if (pointedItem != null && pointedItem.DataContext is string fileName)
                {
                    if (_expandableFileView.IsChildItemMarker(fileName, out var _childInfo))
                    {
                        var childItem = _expandableFileManager.FindChildItem(_childInfo.parentPath, _childInfo.name, _childInfo.level);
                        if (childItem != null)
                        {
                            _controller.ShowChildItemContextMenu(childItem, _fileList);
                        }
                    }
                    else
                    {
                        _controller.ShowFileContextMenu(fileName, _fileList);
                    }
                }
                else
                {
                    _controller.ShowEmptyAreaContextMenu(_fileList);
                }

                e.Handled = true;
                return;
            }

            var item = _fileList.SelectedItem as string;
            if (item == null) return;

            if (_expandableFileView.IsChildItemMarker(item, out var childInfo))
            {
                var childItem = _expandableFileManager.FindChildItem(childInfo.parentPath, childInfo.name, childInfo.level);
                if (childItem != null)
                {
                    _expandableFileView.HandleChildItemClick(childItem, e);
                    e.Handled = true;
                    return;
                }
            }

            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                var visual = e.Source as Visual;
                if (visual != null)
                {
                    var clickedItem = visual.DataContext as string;

                    if (clickedItem != null && clickedItem == _selectedFile)
                    {
                        string fullPath = Path.Combine(_controller.CurrentPath, _selectedFile);
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
        }

        private void OnListBoxItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed &&
                sender is ListBoxItem item &&
                item.DataContext is string fileName)
            {
                _controller.StartDragOperation(item, fileName, e);
            }
        }

        public ListBoxItem GetListBoxItemByFileName(string fileName)
        {
            return _fileList.GetLogicalChildren()
                .OfType<ListBoxItem>()
                .FirstOrDefault(x => x.DataContext as string == fileName);
        }

        public string SelectedItem => _fileList.SelectedItem as string;
        public int SelectedIndex => _fileList.SelectedIndex;
    }
}