using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using System.IO;
using System;


namespace Editor
{
    public class ExplorerContextMenu
    {
        private readonly ExplorerController _controller;
        private readonly List<DescriptionCustomContextMenu> _customContextMenus;
        private readonly ContextMenu _directoryContextMenu;
        private readonly ContextMenu _fileContextMenu;

        public ExplorerContextMenu(ExplorerController controller, List<DescriptionCustomContextMenu> customContextMenus)
        {
            _controller = controller;
            _customContextMenus = customContextMenus;
            _directoryContextMenu = CreateDirectoryContextMenu();
            _fileContextMenu = CreateDefaultFileContextMenu();
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
                    },
                }
            };

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
                            Classes = { "explorerMenu", "explorerMenuItem" },
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
                                Classes = { "explorerMenu", "explorerMenuItem" },
                            };

                            root.Items.Add(new_);
                            root = new_;
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

            return menu;
        }
        public void ShowDirectoryContextMenu(Control target)
        {
            _directoryContextMenu.PlacementTarget = target;
            _directoryContextMenu?.Open(_controller);
        }

        public void ShowFileContextMenu(string fileName, Control target)
        {
            ContextMenu contextMenu = null;
            bool isThereExtension = fileName.IndexOf(".") > -1;
            if (isThereExtension)
            {
                string extension = Path.GetExtension(fileName);
                if (_customContextMenus.Any(menu => menu.Extension == extension))
                {
                    var clipboardPath = Path.Combine(_controller.CurrentPath, fileName);
                    contextMenu = CreateFileContextMenu(fileName, clipboardPath);
                }
                else
                    contextMenu = _fileContextMenu;
            }
            else
            {
                contextMenu = _fileContextMenu;
            }

            contextMenu.PlacementTarget = target;
            contextMenu?.Open(_controller);
        }

        public void ShowChildItemContextMenu(ExpandableFileItemChild childItem, Control target)
        {
            var contextMenu = CreateChildItemContextMenu(childItem);
            contextMenu.PlacementTarget = target;
            contextMenu.Open(_controller);
        }

        public void RegisterCustomContextMenu(DescriptionCustomContextMenu description)
        {
            if (!_customContextMenus.Contains(description))
                _customContextMenus.Add(description);
        }

        public ContextMenu DirectoryContextMenu => _directoryContextMenu;

        public ContextMenu FileContextMenu => _fileContextMenu;
    }
}