using System.Collections.Generic;
using Editor.Utils.Generator;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    internal class MainWindowUIManager
    {
        private DraggableWindowManagerService _windowService;
        private SceneManager _sceneManager;
        private MainWindow _mainWindow;

        private List<IWindowed> _controls = new List<IWindowed>();
        private ConsoleController _consoleController;
        private SceneViewController _sceneViewController;
        private HierarchyController _hierarchyController;
        private WorldController _worldController;
        private InspectorController _inspectorController;
        private ExplorerController _explorerController;

        public MainWindowUIManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.OnSceneInitialize += (e) =>
            {
                _hierarchyController?.UpdateHyerarchy(e);
                _worldController?.UpdateWorlds(e);
                _inspectorController?.Redraw();
                _explorerController?.Redraw();
                _sceneViewController?.SetScene(_sceneManager.CurrentScene);
            };

            _sceneManager.OnSceneChange += (e) =>
            {
                _hierarchyController?.UpdateHyerarchy(e);
                _worldController?.UpdateWorlds(e);
                _inspectorController?.Redraw();
                _explorerController?.Redraw();
                _sceneViewController?.SetScene(_sceneManager.CurrentScene);
            };
            _windowService = ServiceHub.Get<DraggableWindowManagerService>();
            RegisterControllersHandlers();
        }

        private void RegisterControllersHandlers()
        {
            _windowService.RegisterOpenHandler(MainControllers.Hierarchy, controller =>
            {
                var hierarchyController = (HierarchyController)controller;
                hierarchyController.Open();
            });
            _windowService.RegisterOpenHandler(MainControllers.Inspector, controller =>
            {
                var inspectorController = (InspectorController)controller;
                inspectorController.Open();
            });
            _windowService.RegisterOpenHandler(MainControllers.World, controller =>
            {
                var worldController = (WorldController)controller;
                worldController.Open();
            });
            _windowService.RegisterOpenHandler(MainControllers.Explorer, controller =>
            {
                var explorerController = (ExplorerController)controller;
                explorerController.Open();
            });
            _windowService.RegisterOpenHandler(MainControllers.Console, controller =>
            {
                var consoleController = (ConsoleController)controller;
                consoleController.Open();
            });
            _windowService.RegisterOpenHandler(MainControllers.SceneRender, controller =>
            {
                var sceneViewController = (SceneViewController)controller;
                sceneViewController.Open();
            });

            _windowService.RegisterCloseHandler(MainControllers.Hierarchy, controller =>
            {
                var hierarchyController = (HierarchyController)controller;
                hierarchyController.Close();
            });
            _windowService.RegisterCloseHandler(MainControllers.Inspector, controller =>
            {
                var inspectorController = (InspectorController)controller;
                inspectorController.Close();
            });
            _windowService.RegisterCloseHandler(MainControllers.World, controller =>
            {
                var worldController = (WorldController)controller;
                worldController.Close();
            });
            _windowService.RegisterCloseHandler(MainControllers.Explorer, controller =>
            {
                var explorerController = (ExplorerController)controller;
                explorerController.Close();
            });
            _windowService.RegisterCloseHandler(MainControllers.Console, controller =>
            {
                var consoleController = (ConsoleController)controller;
                consoleController.Close();
            });
            _windowService.RegisterCloseHandler(MainControllers.SceneRender, controller =>
            {
                var sceneViewController = (SceneViewController)controller;
                sceneViewController.Close();
            });
        }

        public void RegisterController(MainControllers controllerType, Control controller)
        {
            _windowService.RegisterController(controllerType, controller);

            switch (controllerType)
            {
                case MainControllers.Hierarchy:
                    _hierarchyController = (HierarchyController)controller;
                    _controls.Add(_hierarchyController);
                    break;
                case MainControllers.World:
                    _worldController = (WorldController)controller;
                    _controls.Add(_worldController);
                    break;
                case MainControllers.Inspector:
                    _inspectorController = (InspectorController)controller;
                    _controls.Add(_inspectorController);
                    break;
                case MainControllers.Explorer:
                    _explorerController = (ExplorerController)controller;
                    _controls.Add(_explorerController);
                    break;
                case MainControllers.Console:
                    _consoleController = (ConsoleController)controller;
                    _controls.Add(_consoleController);
                    break;
                case MainControllers.SceneRender:
                    _sceneViewController = (SceneViewController)controller;
                    _controls.Add(_sceneViewController);
                    break;
            }
        }

        public void Initialize()
        {
            InitializeHierarchy();
            InitializeWorld();
            InitializeInspector();
            InitializeExplorer();
            InitializeConsole();
        }
        
        private void InitializeConsole()
        {
            _consoleController.RegisterConsoleCommand(
                new ConsoleCommand
                {
                    CommandName = "exclude",
                    Description = "Exclude extension file from explorer browser",
                    Action = (e) =>
                    {
                        _explorerController.ExcludeFileExtension(e);
                    }
                });
        }
        private void InitializeHierarchy()
        {
            _hierarchyController.EntityCreated += (s, entityName) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.AddEntity(entityName);
                UpdateHyerarchy();
                CleanInspector();
            };

            _hierarchyController.EntityDuplicated += (s, entityName) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.AddDuplicateEntity(entityName);
                UpdateHyerarchy();
                CleanInspector();
            };

            _hierarchyController.EntityRenamed += (s, entity) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.RenameEntity(entity);
                CleanInspector();
            };

            _hierarchyController.EntityDeleted += (s, entity) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.DeleteEntity(entity);
                CleanInspector();
            };

            _hierarchyController.EntitySelected += (s, entity) =>
            {
                IInspectable inspectable = ServiceHub.Get<InspectorDistributor>().GetInspectable(entity);
                if (inspectable != null) _inspectorController.Inspect(inspectable);
                else _inspectorController.CleanInspected();
            };
        }
        private void InitializeWorld()
        {
            _worldController.WorldRenamed += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.RenameWorld(e);
            };
            _worldController.WorldDeleted += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.RemoveWorld(e);
            };
            _worldController.WorldCreated += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.CreateWorld(e);
            };
            _worldController.WorldSelected += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.CurrentScene.SelecteWorld(e);
                UpdateHyerarchy();
            };
        }
        private void InitializeInspector() { }
        private void InitializeExplorer()
        {
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".txt",
                Name = "Read",
                Description = "sd",
                Action = (e) => DebLogger.Debug($"{e}"),
                SubCategory = new string[] { "sub1", "sub2" }
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".cs",
                Name = "Open in IDE",
                Description = "Open file in IDE",
                Action = (e) =>
                {
                    ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE(e.FileFullPath);
                },
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Open in IDE",
                Description = "Open file in IDE",
                Action = (e) =>
                {
                    ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE(e.FileFullPath);
                },
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Generate C#",
                Description = "Generate c sharp view glsl code",
                Action = (e) =>
                {
                    GlslCodeGenerator.GenerateCode(e.FileFullPath, e.FilePath);
                }
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Try To Compile",
                Description = "Checking to compiling code",
                Action = (e) =>
                {
                    var result = GlslCodeGenerator.TryToCompile(e);
                    if (result.Success) DebLogger.Info(result);
                    else DebLogger.Error(result);
                }
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".cs",
                Name = "Create Material",
                Description = "Create material from shader representation",
                Action = (e) =>
                {
                    if (e.FileName.EndsWith("Representation.g.cs"))
                    {
                        var metadata = ServiceHub.Get<MetadataManager>().GetMetadata(e.FileFullPath);

                        var materialController = new MaterialManager();
                        var material = materialController.CreateMaterial(metadata.Guid);

                        string materialPath = Path.Combine(
                            Path.GetDirectoryName(e.FileFullPath),
                            $"{Path.GetFileNameWithoutExtension(e.FileName).Replace("Representation.g", "")}_Material.mat"
                        );

                        materialController.SaveMaterial(material, materialPath);
                        Status.SetStatus($"Created material: {material.Name}");
                    }
                }
            });

            _explorerController.FileSelected += (fileData) =>
            {
                IInspectable inspectable = ServiceHub.Get<InspectorDistributor>().GetInspectable(fileData);
                if (inspectable != null) _inspectorController.Inspect(inspectable);
                else _inspectorController.CleanInspected();
            };
        }

        public void OpenWindow(MainControllers mainControllers) => _windowService.OpenWindow(mainControllers);
        public T GetControl<T>() where T : Control, IWindowed
        {
            var type = typeof(T);
            return (T)_controls.Where(e => e.GetType() == type).FirstOrDefault();
        }

        private void CleanInspector()
        {
            _inspectorController.CleanInspected();
        }
        private void UpdateHyerarchy()
        {
            _hierarchyController.UpdateHyerarchy(_sceneManager.CurrentScene);
        }
        public void RedrawControllers()
        {
            foreach (var controller in _controls)
                controller.Redraw();
        }

        internal void Start()
        {
            _windowService.OpenStartedWindow();
        }
    }
}
