﻿using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using EngineLib;
using Avalonia.Threading;
using OpenglLib;

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
        private GlslEditorController _glslEditorController;

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
                case MainControllers.GlslEditor:
                    _glslEditorController = (GlslEditorController)controller;
                    _controls.Add(_glslEditorController);
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
            #region C#
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".cs",
                Name = "Open in IDE",
                Description = "Open file in IDE",
                Action = (e) =>
                {
                    ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE(e.FileFullPath);
                },
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".cs",
                Name = "Create Material",
                Description = "Create material from shader representation",
                SubCategory = new string[] { "Generate" },
                Action = (e) =>
                {
                    if (e.FileName.EndsWith($"{GeneratorConst.LABLE}.cs"))
                    {
                        var metadata = ServiceHub.Get<EditorMetadataManager>().GetMetadata(e.FileFullPath);

                        var materialController = ServiceHub.Get<EditorMaterialAssetManager>();
                        var material = materialController.CreateMaterialAsset(metadata.Guid);
                    }
                }
            });
            #endregion
            #region RS
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".rs",
                Name = "Onep with GLSL editor",
                Description = "Open with GLSL editor",
                SubCategory = new string[] { "Shader" },
                Action = (e) =>
                {
                    OpenWindow(MainControllers.GlslEditor);
                    _glslEditorController.OpenFile(e.FileFullPath);
                },
            });
            #endregion
            #region GLSL
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Open with IDE",
                Description = "Open file in IDE",
                SubCategory = new string[] { "Shader" },
                Action = (e) =>
                {
                    ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE(e.FileFullPath);
                },
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Generate C# ",
                Description = "Generate C# Representation",
                SubCategory = new string[] { "Shader" },
                Action = async (e) =>
                {
                    CsCompileWatcher csCompiler = ServiceHub.Get<CsCompileWatcher>();
                    csCompiler.EnableWatching(false);
                    try
                    {
                        var loadingManager = ServiceHub.Get<LoadingManager>();
                        await loadingManager.RunWithLoading(async (progress) =>
                        {
                            await Task.Delay(100);
                            progress.Report((0, $"Generating from {e.FileName}..."));
                            string outputDirectoryName = e.FileName.Contains(".") ? e.FileName.Substring(0, e.FileName.IndexOf(".")) : e.FileName;
                            outputDirectoryName = Path.Combine(e.FilePath, outputDirectoryName);

                            string assetpath = ServiceHub.Get<DirectoryExplorer>().GetPath<AssetsDirectory>();
                            FileEvent fileEvent = new FileEvent();
                            fileEvent.FileFullPath = e.FileFullPath;
                            fileEvent.FileName = Path.GetFileNameWithoutExtension(e.FileFullPath);
                            fileEvent.FileExtension = Path.GetExtension(e.FileFullPath);
                            fileEvent.FilePath = e.FileFullPath.Substring(assetpath.Length);

                            var result = GlslCompiler.TryToCompile(fileEvent);
                            if (result.Success)
                            {
                                progress.Report((90, $"Generating from {e.FileName}..."));
                                await Task.Delay(1000);

                                DebLogger.Info(result.Log);
                                ShaderUniformCacheData uniformCacheData = new ShaderUniformCacheData()
                                {
                                    AttributeLocations = result.AttributeLocations,
                                    UniformLocations = result.UniformLocations,
                                    UniformBlocks = result.UniformBlocks,
                                    UniformInfo = result.UniformInfo,
                                };
                                await GlslCodeGenerator.GenerateCode(e.FileFullPath, outputDirectoryName, uniformCacheData);
                            }
                            else
                            {
                                DebLogger.Error(result.Log);
                            }

                        });
                        ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                        await ServiceHub.Get<ScriptSyncSystem>().RebuildProject(pConf.BuildType);
                    }
                    catch(Exception err)
                    {
                        DebLogger.Error($"Generating error {e.FilePath} \n {err.Message}");
                    }
                    finally
                    {
                        await Task.Delay(1000);
                        csCompiler.EnableWatching(false);
                    }
                }
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Try To Compile",
                Description = "Checking to compiling code",
                SubCategory = new string[] { "Shader" },
                Action = (e) =>
                {
                    var result = GlslCompiler.TryToCompile(e);
                    if (result.Success) DebLogger.Info(result);
                    else DebLogger.Error(result);
                }
            });
            _explorerController.RegisterCustomContextMenu(new DescriptionFileCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Onep with GLSL editor",
                Description = "Open with GLSL editor",
                SubCategory = new string[] { "Shader" },
                Action = (e) =>
                {
                    OpenWindow(MainControllers.GlslEditor);
                    _glslEditorController.OpenFile(e.FileFullPath);
                }
            });
            #endregion
            #region Material
            _explorerController.RegisterCustomContextMenu(new DescriptionFreeSpaceCustomContextMenu()
            {
                Action = (s) =>
                {
                    ServiceHub.Get<EditorMaterialAssetManager>().CreateEmptyMaterialAsset(s);
                },
                Name = "Material",
                SubCategory= new string[] { "Create" },
                Description = "Ko"
            });
            #endregion
            

            _explorerController.FileSelected += (fileData) =>
            {
                IInspectable inspectable = ServiceHub.Get<InspectorDistributor>().GetInspectable(fileData);
                if (inspectable != null) _inspectorController.Inspect(inspectable);
                else _inspectorController.CleanInspected();

                Dispatcher.UIThread.Invoke(() =>
                {
                    _glslEditorController?.OpenFile(fileData.FileFullPath);
                }, DispatcherPriority.Background);
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

        internal void Dispose()
        {
            _sceneViewController?.Dispose();
        }
    }
}
