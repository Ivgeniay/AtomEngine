using System.Collections.ObjectModel;
using System.Collections.Generic;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using System.IO;
using Avalonia;
using System;
using MouseButton = Avalonia.Input.MouseButton;


namespace Editor
{
    public class ExplorerTreeView
    {
        private readonly ExplorerController _controller;
        private readonly TreeView _treeView;
        private readonly ObservableCollection<TreeViewItem> _treeItems;
        private readonly HashSet<string> _expandedPaths;
        private readonly string _rootPath;

        public event Action<string> DirectorySelected;

        public ExplorerTreeView(ExplorerController controller, TreeView treeView, string rootPath)
        {
            _controller = controller;
            _treeView = treeView;
            _rootPath = rootPath;
            _treeItems = new ObservableCollection<TreeViewItem>();
            _expandedPaths = new HashSet<string>();

            Initialize();
        }

        private void Initialize()
        {
            _treeView.ItemsSource = _treeItems;
            _treeView.SelectionChanged += OnTreeViewSelectionChanged;
            _treeView.PointerReleased += OnTreeViewPointerReleased;

            _treeView.ContainerPrepared += (sender, e) =>
            {
                if (e.Container is TreeViewItem item)
                {
                    string path = item.Tag as string;
                    if (path != null)
                    {
                        bool shouldExpand = _expandedPaths.Contains(path) ||
                                        path == _controller.CurrentPath ||
                                        _controller.CurrentPath.StartsWith(path + Path.DirectorySeparatorChar);

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

            DragDrop.SetAllowDrop(_treeView, true);
            _treeView.AddHandler(DragDrop.DragOverEvent, OnTreeViewDragOver);
            _treeView.AddHandler(DragDrop.DropEvent, OnTreeViewDrop);

            RefreshTreeView();
        }

        public void RefreshTreeView()
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
                                  path == _controller.CurrentPath ||
                                  _controller.CurrentPath.StartsWith(path + Path.DirectorySeparatorChar);

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

        private void OnTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0 && e.AddedItems[0] is TreeViewItem selected)
            {
                string path = selected.Tag as string;
                if (path != null && Directory.Exists(path))
                {
                    DirectorySelected?.Invoke(path);
                }
            }
        }

        private void OnTreeViewPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                _controller.ShowDirectoryContextMenu(_treeView);
                e.Handled = true;
            }
        }

        public TreeViewItem FindTreeViewItemByPath(string path)
        {
            foreach (var item in _treeView.GetLogicalDescendants().OfType<TreeViewItem>())
            {
                if (item.Tag as string == path)
                {
                    return item;
                }
            }
            return null;
        }

        public TreeViewItem FindTreeViewItemAtPosition(Point position)
        {
            Visual visual = _treeView.InputHitTest(position) as Visual;

            while (visual != null)
            {
                if (visual is TreeViewItem item)
                    return item;

                visual = visual.GetVisualAncestors().FirstOrDefault();
            }

            return null;
        }

        private void OnTreeViewDragOver(object sender, DragEventArgs e)
        {
            _controller.HandleTreeViewDragOver(sender, e, _treeView);
        }

        private void OnTreeViewDrop(object sender, DragEventArgs e)
        {
            _controller.HandleTreeViewDrop(sender, e, _treeView);
        }

        public TreeViewItem SelectedItem => _treeView.SelectedItem as TreeViewItem;
    }
}