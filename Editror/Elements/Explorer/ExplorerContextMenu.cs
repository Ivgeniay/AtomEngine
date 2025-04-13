using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using System.IO;
using System;
using EngineLib;


namespace Editor
{
    public class ExplorerContextMenu
    {
        private readonly ExplorerController _controller;

        private readonly List<DescriptionFileCustomContextMenu> _customFileContextMenus;
        private readonly List<DescriptionFreeSpaceCustomContextMenu> _customFreeSpaceContextMenus;
        private readonly List<DescriptionDirectoryTreeCustomContextMenu> _customDirectoryContextMenus;

        private ContextMenu _currentActiveMenu;


        public ExplorerContextMenu(
            ExplorerController controller,
            List<DescriptionFileCustomContextMenu> customFileContextMenus,
            List<DescriptionFreeSpaceCustomContextMenu> customFreeSpaceContextMenus,
            List<DescriptionDirectoryTreeCustomContextMenu> customDirectoryContextMenus)
        {
            _controller = controller;

            _customFileContextMenus = customFileContextMenus;
            _customFreeSpaceContextMenus = customFreeSpaceContextMenus;
            _customDirectoryContextMenus = customDirectoryContextMenus;
        }

        private void CloseAllMenus()
        {
            if (_currentActiveMenu != null)
            {
                _currentActiveMenu.Close();
                _currentActiveMenu = null;
            }
        }

        public ContextMenu CreateDirectoryContextMenu()
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
                    Command = new Command(_controller.OnNewFolderCommand)
                },
                new MenuItem
                {
                    Header = "New File",
                    Classes = { "explorerMenuItem" },
                    Command = new Command(_controller.OnNewFileCommand)
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
                    Command = new Command(_controller.OnCutDirectoryCommand)
                },
                new MenuItem
                {
                    Header = "Copy",
                    Classes = { "explorerMenuItem" },
                    Command = new Command(_controller.OnCopyDirectoryCommand)
                },
                new MenuItem
                {
                    Header = "Paste",
                    Classes = { "explorerMenuItem" },
                    Command = new Command(_controller.OnPasteCommand)
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
                    Command = new Command(_controller.OnDeleteDirectoryCommand)
                },
                new MenuItem
                {
                    Header = "Rename",
                    Classes = { "explorerMenuItem" },
                    Command = new Command(_controller.OnRenameDirectoryCommand)
                }
            }
            };

            if (_customDirectoryContextMenus.Any())
            {
                menu.Items.Insert(0, new MenuItem
                {
                    Header = "-",
                    Classes = { "explorerMenuSeparator" }
                });

                Dictionary<string, MenuItem> categoryMenuItems = new Dictionary<string, MenuItem>();

                foreach (var customMenu in _customDirectoryContextMenus)
                {
                    if (customMenu.SubCategory != null && customMenu.SubCategory.Length > 0)
                    {
                        var command = new Command(() => customMenu.Action?.Invoke(_controller.CurrentPath));

                        MenuItem finalItem = new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenu", "explorerMenuItem" },
                            Command = command
                        };

                        var parentCollection = menu.Items;
                        MenuItem currentParent = null;
                        string categoryPath = string.Empty;

                        foreach (var category in customMenu.SubCategory)
                        {
                            categoryPath = string.IsNullOrEmpty(categoryPath) ?
                                category : $"{categoryPath}|{category}";

                            if (!categoryMenuItems.TryGetValue(categoryPath, out MenuItem categoryItem))
                            {
                                categoryItem = new MenuItem
                                {
                                    Header = category,
                                    Classes = { "explorerMenu", "explorerMenuItem" }
                                };
                                if (currentParent == null)
                                {
                                    parentCollection.Insert(0, categoryItem);
                                }
                                else
                                {
                                    currentParent.Items.Add(categoryItem);
                                }
                                categoryMenuItems[categoryPath] = categoryItem;
                            }
                            currentParent = categoryItem;
                            parentCollection = categoryItem.Items;
                        }

                        if (currentParent != null)
                        {
                            currentParent.Items.Add(finalItem);
                        }
                    }
                    else
                    {
                        menu.Items.Insert(0, new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenuItem" },
                            Command = new Command(() => customMenu.Action?.Invoke(_controller.CurrentPath)),
                        });
                    }
                }
            }

            return menu;
        }

        public ContextMenu CreateDefaultFileContextMenu()
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
                        Command = new Command(_controller.OnOpenCommand)
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
                        Command = new Command(_controller.OnCutFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "Copy",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(_controller.OnCopyFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "Paste",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(_controller.OnPasteCommand)
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
                        Command = new Command(_controller.OnDeleteFileCommand)
                    },
                    new MenuItem
                    {
                        Header = "Rename",
                        Classes = { "explorerMenuItem" },
                        Command = new Command(_controller.OnRenameFileCommand)
                    },
                }
            };

            return menu;
        }

        public ContextMenu CreateFileContextMenu(string filename, string path)
        {
            var contextMenu = CreateDefaultFileContextMenu();
            string extension = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(extension))
                return contextMenu;

            var customMenus = _customFileContextMenus
                .Where(menu => menu.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (customMenus.Any())
            {
                contextMenu.Items.Insert(0, new MenuItem
                {
                    Header = "-",
                    Classes = { "explorerMenuSeparator" }
                });

                Dictionary<string, MenuItem> categoryMenuItems = new Dictionary<string, MenuItem>();

                foreach (var customMenu in customMenus)
                {
                    if (customMenu.SubCategory != null && customMenu.SubCategory.Length > 0)
                    {
                        var command = new Command(() => customMenu.Action?.Invoke(new FileSelectionEvent
                        {
                            FileName = filename,
                            FileFullPath = path,
                            FilePath = path.Substring(0, path.IndexOf(filename)),
                            FileExtension = extension
                        }));

                        MenuItem finalItem = new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenu", "explorerMenuItem" },
                            Command = command
                        };

                        var parentCollection = contextMenu.Items;
                        MenuItem currentParent = null;
                        string categoryPath = string.Empty;

                        foreach (var category in customMenu.SubCategory)
                        {
                            categoryPath = string.IsNullOrEmpty(categoryPath) ?
                                category : $"{categoryPath}|{category}";

                            if (!categoryMenuItems.TryGetValue(categoryPath, out MenuItem categoryItem))
                            {
                                categoryItem = new MenuItem
                                {
                                    Header = category,
                                    Classes = { "explorerMenu", "explorerMenuItem" }
                                };
                                if (currentParent == null)
                                {
                                    parentCollection.Insert(0, categoryItem);
                                }
                                else
                                {
                                    currentParent.Items.Add(categoryItem);
                                }
                                categoryMenuItems[categoryPath] = categoryItem;
                            }
                            currentParent = categoryItem;
                            parentCollection = categoryItem.Items;
                        }

                        if (currentParent != null)
                        {
                            currentParent.Items.Add(finalItem);
                        }
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
            contextMenu.Closed += OnMenuClosed;

            return contextMenu;
        }

        public ContextMenu CreateChildItemContextMenu(ExpandableFileItemChild childItem)
        {
            var menu = new ContextMenu
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
            menu.Closed += OnMenuClosed;

            return menu;
        }

        public ContextMenu CreateEmptyAreaContextMenu()
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
                    Command = new Command(_controller.OnNewFolderCommand)
                },
                new MenuItem
                {
                    Header = "New File",
                    Classes = { "explorerMenuItem" },
                    Command = new Command(_controller.OnNewFileCommand)
                },
                new MenuItem
                {
                    Header = "-",
                    Classes = { "explorerMenuSeparator" }
                },
                new MenuItem
                {
                    Header = "Paste",
                    Classes = { "explorerMenuItem" },
                    Command = new Command(_controller.OnPasteCommand)
                }
            }
            };

            if (_customFreeSpaceContextMenus.Any())
            {
                menu.Items.Insert(0, new MenuItem
                {
                    Header = "-",
                    Classes = { "explorerMenuSeparator" }
                });

                Dictionary<string, MenuItem> categoryMenuItems = new Dictionary<string, MenuItem>();

                foreach (var customMenu in _customFreeSpaceContextMenus)
                {
                    if (customMenu.SubCategory != null && customMenu.SubCategory.Length > 0)
                    {
                        var command = new Command(() => customMenu.Action?.Invoke(_controller.CurrentPath));

                        MenuItem finalItem = new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenu", "explorerMenuItem" },
                            Command = command
                        };

                        var parentCollection = menu.Items;
                        MenuItem currentParent = null;
                        string categoryPath = string.Empty;

                        foreach (var category in customMenu.SubCategory)
                        {
                            categoryPath = string.IsNullOrEmpty(categoryPath) ?
                                category : $"{categoryPath}|{category}";

                            if (!categoryMenuItems.TryGetValue(categoryPath, out MenuItem categoryItem))
                            {
                                categoryItem = new MenuItem
                                {
                                    Header = category,
                                    Classes = { "explorerMenu", "explorerMenuItem" }
                                };
                                if (currentParent == null)
                                {
                                    parentCollection.Insert(0, categoryItem);
                                }
                                else
                                {
                                    currentParent.Items.Add(categoryItem);
                                }
                                categoryMenuItems[categoryPath] = categoryItem;
                            }
                            currentParent = categoryItem;
                            parentCollection = categoryItem.Items;
                        }

                        if (currentParent != null)
                        {
                            currentParent.Items.Add(finalItem);
                        }
                    }
                    else
                    {
                        menu.Items.Insert(0, new MenuItem
                        {
                            Header = customMenu.Name,
                            Classes = { "explorerMenuItem" },
                            Command = new Command(() => customMenu.Action?.Invoke(_controller.CurrentPath)),
                        });
                    }
                }
            }

            return menu;
        }


        private void OnMenuClosed(object? sender, EventArgs e)
        {
            if (_currentActiveMenu == sender)
            {
                _currentActiveMenu = null;
            }
        }


        public void ShowDirectoryContextMenu(Control target)
        {
            CloseAllMenus();
            var menu = CreateDirectoryContextMenu();
            menu.PlacementTarget = target;
            menu.Open(_controller);
            _currentActiveMenu = menu;
        }

        public void ShowFileContextMenu(string fileName, Control target)
        {
            CloseAllMenus();
            ContextMenu contextMenu = null;
            bool isThereExtension = fileName.IndexOf(".") > -1;

            if (isThereExtension)
            {
                string extension = Path.GetExtension(fileName);
                if (_customFileContextMenus.Any(menu => menu.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    var clipboardPath = Path.Combine(_controller.CurrentPath, fileName);
                    contextMenu = CreateFileContextMenu(fileName, clipboardPath);
                }
                else
                    contextMenu = CreateDefaultFileContextMenu();
            }
            else
            {
                contextMenu = CreateDefaultFileContextMenu();
            }

            contextMenu.PlacementTarget = target;
            contextMenu.Open(_controller);
            _currentActiveMenu = contextMenu;
        }

        public void ShowChildItemContextMenu(ExpandableFileItemChild childItem, Control target)
        {
            CloseAllMenus();
            var contextMenu = CreateChildItemContextMenu(childItem);
            contextMenu.PlacementTarget = target;
            contextMenu.Open(_controller);
            _currentActiveMenu = contextMenu;
        }

        public void ShowEmptyAreaContextMenu(Control target)
        {
            CloseAllMenus();
            var menu = CreateEmptyAreaContextMenu();
            menu.PlacementTarget = target;
            menu.Open(_controller);
            _currentActiveMenu = menu;
        }


        public void RegisterFileCustomContextMenu(DescriptionFileCustomContextMenu description)
        {
            if (!_customFileContextMenus.Contains(description))
                _customFileContextMenus.Add(description);
        }

        public void RegisterFreeSpaceCustomContextMenu(DescriptionFreeSpaceCustomContextMenu description)
        {
            if (!_customFreeSpaceContextMenus.Contains(description))
                _customFreeSpaceContextMenus.Add(description);
        }

        public void RegisterDirectoryCustomContextMenu(DescriptionDirectoryTreeCustomContextMenu description)
        {
            if (!_customDirectoryContextMenus.Contains(description))
                _customDirectoryContextMenus.Add(description);
        }

    }
}