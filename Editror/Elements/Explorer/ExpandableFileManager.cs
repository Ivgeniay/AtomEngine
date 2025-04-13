using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using EngineLib;


namespace Editor
{
    public class ExpandableFileManager
    {
        private List<ExpandableFileItem> _expandableFileItems = new List<ExpandableFileItem>();
        private Dictionary<string, List<ExpandableFileItemChild>> _expandedFiles = new Dictionary<string, List<ExpandableFileItemChild>>();


        public event Action StateChanged;

        public void RegisterHandler(ExpandableFileItem handler)
        {
            if (handler != null && !_expandableFileItems.Contains(handler))
                _expandableFileItems.Add(handler);
        }

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

        public bool CanExpandFile(string filePath)
        {
            return GetExpandableHandler(filePath) != null;
        }
        
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

        public bool IsFileExpanded(string filePath)
        {
            return _expandedFiles.ContainsKey(filePath);
        }

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

        public IEnumerable<ExpandableFileItemChild> GetChildItems(string filePath)
        {
            if (_expandedFiles.TryGetValue(filePath, out var children))
                return children;

            return Enumerable.Empty<ExpandableFileItemChild>();
        }

        public void ToggleChildItemExpansion(ExpandableFileItemChild item)
        {
            if (item == null) return;

            item.IsExpanded = !item.IsExpanded;
            StateChanged?.Invoke();
        }

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
}