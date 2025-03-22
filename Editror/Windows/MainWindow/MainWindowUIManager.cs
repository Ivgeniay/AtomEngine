using System.Collections.Generic;
using Editor.Utils.Generator;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using EngineLib;

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
        private SystemDependencyController _nodeGraphController;

        public MainWindowUIManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _sceneManager = ServiceHub.Get<SceneManager>();
            _windowService = ServiceHub.Get<DraggableWindowManagerService>();

            RegisterControllersHandlers();
        }

        private void RegisterControllersHandlers()
        {
            var mainControllers = Enum.GetValues<MainControllers>();
            foreach (var controllerName in mainControllers)
            {
                _windowService.RegisterOpenHandler(controllerName, controller => 
                { 
                    if (controller is IWindowed windowed) windowed.Open(); 
                });
                _windowService.RegisterCloseHandler(controllerName, controller => 
                { 
                    if (controller is IWindowed windowed) windowed.Close(); 
                });
            }
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
                case MainControllers.Systems:
                    _nodeGraphController = (SystemDependencyController)controller;
                    _controls.Add(_nodeGraphController);
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
            InitializrSceneView();
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
                _sceneManager.AddEntity(entityName);
                CleanInspector();
            };

            _hierarchyController.EntityDuplicated += (s, entity) =>
            {
                Select.DeSelectAll();
                _sceneManager.AddDuplicateEntity(entity);
                CleanInspector();
            };

            _hierarchyController.EntityRenamed += (s, entity) =>
            {
                Select.DeSelectAll();
                _sceneManager.RenameEntity(entity);
                CleanInspector();
            };

            _hierarchyController.EntityDeleted += (s, entity) =>
            {
                Select.DeSelectAll();
                _sceneManager.RemoveEntity(entity);
                CleanInspector();
            };

            _hierarchyController.EntitySelected += (s, entity) =>
            {
                IInspectable inspectable = ServiceHub.Get<InspectorDistributor>().GetInspectable(entity);
                if (inspectable != null) _inspectorController.Inspect(inspectable);
                else _inspectorController.CleanInspected();
                Select.SelectItem(entity.Id);
            };

            _hierarchyController.EntityReordered += _sceneManager.EntityReordered;
        }
        private void InitializeWorld()
        {
            _worldController.WorldRenamed += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.RenameWorld(e);
            };
            _worldController.WorldDeleted += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.RemoveWorld(e);
            };
            _worldController.WorldCreated += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.CreateWorld(e);
            };
            _worldController.WorldSelected += (sender, e) =>
            {
                Select.DeSelectAll();
                _sceneManager.SelecteWorld(e);
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
                Name = "Generate All",
                Description = "Generate c sharp view glsl code",
                Action = (e) =>
                {
                    //ShaderCodeGenerationManager.GenerateAllShadersAndComponents(e.FileFullPath, e.FilePath);
                    ShaderCodeGenerationManager.GenerateAllShadersAndComponents(e.FilePath, e.FilePath);
                }
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Try To Compile",
                Description = "Checking to compiling code",
                Action = (e) =>
                {
                    var result = GlslCompiler.TryToCompile(e);
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
                        var metadata = ServiceHub.Get<EditorMetadataManager>().GetMetadata(e.FileFullPath);

                        var materialController = new EditorMaterialCacher();
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
        private void InitializrSceneView()
        {
            _sceneViewController.OnEntitySelected += (e) =>
            {
                _hierarchyController.SelectEntity(e);
            };

        }
        public void OpenWindow(MainControllers mainControllers)
        {

            _windowService.OpenWindow(mainControllers);
        }

        public T GetControl<T>() where T : Control, IWindowed
        {
            var type = typeof(T);
            return (T)_controls.Where(e => e.GetType() == type).FirstOrDefault();
        }

        private void CleanInspector()
        {
            _inspectorController.CleanInspected();
        }

        internal void OpenCachedWindows()
        {
            _windowService.OpenStartedWindow();
        }
    }
}
