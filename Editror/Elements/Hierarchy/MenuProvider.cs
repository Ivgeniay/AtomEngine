using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using Avalonia.VisualTree;

namespace Editor
{
    internal class MenuProvider
    {
        private readonly HierarchyController _controller;
        private ContextMenu _backgroundContextMenu;
        private ContextMenu _entityContextMenu;
        private EntityHierarchyOperations _operations;

        public MenuProvider(HierarchyController controller)
        {
            _controller = controller;
            _operations = new EntityHierarchyOperations(controller);

            _backgroundContextMenu = CreateBackgroundContextMenu();
            _entityContextMenu = CreateEntityContextMenu();
        }

        public ContextMenu CreateBackgroundContextMenu()
        {
            var backgroundMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var createEntityItem = new MenuItem
            {
                Header = "Create Entity",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateNewEntity)
            };

            var separatorItem = new MenuItem
            {
                Header = "-",
                Classes = { "hierarchySeparator" }
            };

            var transform3dItem = new MenuItem
            {
                Header = "3D Object",
                Classes = { "hierarchyMenuItem" }
            };

            var cubeItem = new MenuItem
            {
                Header = "Cube",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateCube)
            };

            var sphereItem = new MenuItem
            {
                Header = "Sphere",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateSphere)
            };

            var capsuleItem = new MenuItem
            {
                Header = "Capsule",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateCapsule)
            };

            var cylinderItem = new MenuItem
            {
                Header = "Cylinder",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreateCylinder)
            };

            var planeItem = new MenuItem
            {
                Header = "Plane",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(CreatePlane)
            };

            transform3dItem.Items.Add(cubeItem);
            transform3dItem.Items.Add(sphereItem);
            transform3dItem.Items.Add(capsuleItem);
            transform3dItem.Items.Add(cylinderItem);
            transform3dItem.Items.Add(planeItem);

            backgroundMenu.Items.Add(createEntityItem);
            backgroundMenu.Items.Add(separatorItem);
            backgroundMenu.Items.Add(transform3dItem);

            return backgroundMenu;
        }

        public ContextMenu CreateEntityContextMenu()
        {
            var entityContextMenu = new ContextMenu
            {
                Classes = { "hierarchyMenu" }
            };

            var renameItem = new MenuItem
            {
                Header = "Rename",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(StartRenamingCommand)
            };

            var duplicateItem = new MenuItem
            {
                Header = "Duplicate",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(DuplicateEntityCommand)
            };

            var deleteItem = new MenuItem
            {
                Header = "Delete",
                Classes = { "hierarchyMenuItem" },
                Command = new Command(DeleteEntityCommand)
            };

            var entitySeparator = new MenuItem
            {
                Header = "-",
                Classes = { "hierarchySeparator" }
            };

            var addComponentItem = new MenuItem
            {
                Header = "Add Component",
                Classes = { "hierarchyMenuItem" }
            };

            var physicsItem = new MenuItem
            {
                Header = "Physics",
                Classes = { "hierarchyMenuItem" }
            };
            var renderingItem = new MenuItem
            {
                Header = "Rendering",
                Classes = { "hierarchyMenuItem" }
            };

            addComponentItem.Items.Add(physicsItem);
            addComponentItem.Items.Add(renderingItem);

            entityContextMenu.Items.Add(renameItem);
            entityContextMenu.Items.Add(duplicateItem);
            entityContextMenu.Items.Add(deleteItem);
            entityContextMenu.Items.Add(entitySeparator);
            entityContextMenu.Items.Add(addComponentItem);

            return entityContextMenu;
        }

        public void OnHierarchyPointerPressed(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetCurrentPoint(_controller);

            if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                _backgroundContextMenu.Open(_controller);
                e.Handled = true;
            }
            else
            {
                _backgroundContextMenu.Close();
            }
        }

        public void OnEntityContextMenuRequested(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetCurrentPoint(null);

            if (point.Properties.IsRightButtonPressed || point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                var element = e.Source as Visual;
                if (element != null)
                {
                    while (element != null && !(element.DataContext is EntityHierarchyItem))
                    {
                        element = element.GetVisualParent();
                    }

                    if (element != null && element.DataContext is EntityHierarchyItem entityItem)
                    {
                        _controller.EntitiesList.SelectedItem = entityItem;
                        _controller.OnEntitySelected(entityItem);

                        _entityContextMenu.Open(_controller);
                        e.Handled = true;
                    }
                }
            }
        }

        private void CreateNewEntity() => _controller.CreateNewEntity(_operations.GetUniqueName("New Entity"));
        private void CreateCube() => _controller.CreateNewEntity(_operations.GetUniqueName("Cube"));
        private void CreateSphere() => _controller.CreateNewEntity(_operations.GetUniqueName("Sphere"));
        private void CreateCapsule() => _controller.CreateNewEntity(_operations.GetUniqueName("Capsule"));
        private void CreateCylinder() => _controller.CreateNewEntity(_operations.GetUniqueName("Cylinder"));
        private void CreatePlane() => _controller.CreateNewEntity(_operations.GetUniqueName("Plane"));

        private void StartRenamingCommand()
        {
            if (_controller.EntitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                _operations.StartRenaming(selectedEntity);
            }
        }

        private void DuplicateEntityCommand()
        {
            if (_controller.EntitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                _operations.DuplicateEntity(selectedEntity);
            }
        }

        private void DeleteEntityCommand()
        {
            if (_controller.EntitiesList.SelectedItem is EntityHierarchyItem selectedEntity)
            {
                _operations.DeleteEntity(selectedEntity);
            }
        }
    }

}