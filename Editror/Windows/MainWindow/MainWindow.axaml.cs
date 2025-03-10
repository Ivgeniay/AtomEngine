
using System.Collections.Generic;
using Editor.Utils.Generator;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using AtomEngine;
using Avalonia;
using System;

namespace Editor
{
#if MY_FEATURE
#endif
    public partial class MainWindow : Window
    {
        public static Canvas MainCanvas_ { get; private set; }
        private MainWindowUIManager _uIManager;
        private SceneManager _sceneManager;

        private EditorToolbar _toolbar;
        private EditorStatusBar _statusBar;
        public MainWindow()
        {
            SystemDecorations = SystemDecorations.Full;
            //ExtendClientAreaToDecorationsHint = true;
            //ExtendClientAreaTitleBarHeightHint = 30;
#if DEBUG
            this.AttachDevTools();
#endif

            InitializeComponent();

            InitializeStatusBar();

            MainCanvas_ = MainCanvas;
            ServiceHub.Get<DraggableWindowManagerService>().SetCanvas(MainCanvas);
            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.SetMainWindow(this);
            _uIManager = new MainWindowUIManager(this);

            var hierarhy = new HierarchyController();
            var inspector = new InspectorController();
            var worlds = new WorldController();
            var console = new ConsoleController();
            var explorer = new ExplorerController();
            var sceneView = new SceneViewController();
            var _nodeGraphController = new NodeGraphController();

            _uIManager.RegisterController(MainControllers.Hierarchy, hierarhy);
            _uIManager.RegisterController(MainControllers.Inspector, inspector);
            _uIManager.RegisterController(MainControllers.World, worlds);
            _uIManager.RegisterController(MainControllers.Console, console);
            _uIManager.RegisterController(MainControllers.Explorer, explorer);
            _uIManager.RegisterController(MainControllers.SceneRender, sceneView);
            _uIManager.RegisterController(MainControllers.SystemGraph, _nodeGraphController);

            _uIManager.Initialize();

            _toolbar = new EditorToolbar(ToolbarContainer);
            ServiceHub.Get<ToolbarService>().RegisterEditorToolbar(_toolbar);

            EditorToolbarCategory fileCathegory = new EditorToolbarCategory()
            {
                Title = "File",
                Description = "File operations",
                Buttons = new List<EditorToolbarButton>()
                {
                    new EditorToolbarButton()
                    {
                        Text = "New",
                        Description = "Create new scene",
                        Action = async () =>
                        {
                            await _sceneManager.HandleNewScene();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Open",
                        Description = "Open scene from drive",
                        Action = async () =>
                        {
                            await _sceneManager.HandleOpenScene();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Save",
                        Description = "Save current scene",
                        Action = async () =>
                        {
                            await _sceneManager.HandleSaveScene();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Save As...",
                        Description = "Save current scene as...",
                        Action = async () =>
                        {
                            await _sceneManager.HandleSaveSceneAs();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Exit",
                        Description = "Close editor",
                        Action = () =>
                        {
                            Close();
                        }
                    }
                }
            };
            EditorToolbarCategory editCategory = new EditorToolbarCategory()
            {
                Title = "Edit",
                Description = "Edition",
                Buttons = new List<EditorToolbarButton>()
                {
                    new EditorToolbarButton()
                    {
                        Text = "Undo",
                        Description = "",
                        Action = () => { DebLogger.Debug("Undo"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Redo",
                        Description = "",
                        Action = () => { DebLogger.Debug("Redo"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Cut",
                        Description = "",
                        Action = () => { DebLogger.Debug("Cut"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Copy",
                        Description = "",
                        Action = () => { DebLogger.Debug("Copy"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Paste",
                        Description = "",
                        Action = () => { DebLogger.Debug("Paste"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Delete",
                        Description = "",
                        Action = () => { DebLogger.Debug("Delete"); }
                    },
                }
            };
            EditorToolbarCategory viewCategory = new EditorToolbarCategory()
            {
                Title = "View",
                Description = "Window manager",
                Buttons = new List<EditorToolbarButton>()
                {
                    new EditorToolbarButton()
                    {
                        Text = "Hierarchy",
                        Description = "",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Hierarchy);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Inspector",
                        Description = "",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Inspector);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "World",
                        Description = "",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.World);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Systems",
                        Description = "",
                        Action = () => {
                            DebLogger.Debug("Systems");
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Scene",
                        Description = "",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.SceneRender);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Game",
                        Description = "",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Game);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Console",
                        Description = "",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Console);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Explorer",
                        Description = "Project file explorer",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Explorer);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "System Graph",
                        Description = "System manager",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.SystemGraph);
                        }
                    },

                }
            };
            EditorToolbarCategory buildCategory = new EditorToolbarCategory()
            {
                Title = "Build",
                Description = "Build category",
                Buttons = new List<EditorToolbarButton>()
                {
                    new EditorToolbarButton()
                    {
                        Text = "Build Project",
                        Description = "",
                        Action = async () => {
                            DebLogger.Debug("Build Project");
                            ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                            await ServiceHub.Get<ScriptSyncSystem>().RebuildProject(pConf.BuildType);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Create Solution",
                        Description = "",
                        Action = () => {
                            DebLogger.Debug("Generate Solution");
                            ServiceHub.Get<ScriptProjectGenerator>().GenerateProject();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Clean",
                        Description = "",
                        Action = () => { DebLogger.Debug("Clean"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Rebuild All",
                        Description = "",
                        Action = async () => {
                            ProjectConfigurations pConf = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                            await ServiceHub.Get<ScriptSyncSystem>().RebuildProject(pConf.BuildType);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Open IDE",
                        Description = "IDE",
                        Action = () =>
                        {
                            ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE();
                        }
                    }
                }
            };
            EditorToolbarCategory toolCategory = new EditorToolbarCategory()
            {
                Title = "Tools",
                Description = "Tools",
                Buttons = new List<EditorToolbarButton>()
                {
                    new EditorToolbarButton()
                    {
                        Text = "Options",
                        Description = "",
                        Action = () => { DebLogger.Debug("Options"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Extensions",
                        Description = "",
                        Action = () => { DebLogger.Debug("Extensions"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Package Manager",
                        Description = "",
                        Action = () => { DebLogger.Debug("Package Manager"); }
                    },
                }
            };
            EditorToolbarCategory helpCategory = new EditorToolbarCategory()
            {
                Title = "Help",
                Description = "Help, FAQ, Documentation",
                Buttons = new List<EditorToolbarButton>()
                {
                    new EditorToolbarButton()
                    {
                        Text = "Documentation",
                        Description = "",
                        Action = () => { DebLogger.Debug("Documentation"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "About",
                        Description = "",
                        Action = () => { DebLogger.Debug("About"); }
                    },
                }
            };

            _toolbar.RegisterCathegory(fileCathegory);
            _toolbar.RegisterCathegory(editCategory);
            _toolbar.RegisterCathegory(viewCategory);
            _toolbar.RegisterCathegory(buildCategory);
            _toolbar.RegisterCathegory(toolCategory);
            _toolbar.RegisterCathegory(helpCategory);

            _sceneManager.HandleNewScene().GetAwaiter().GetResult();
            _uIManager.OpenCachedWindows();

            ServiceHub.Get<AssetFileSystem>().RegisterCommand(new FileEventCommand()
            {
                FileExtension = ".glsl",
                Type = FileEventType.FileChanged,
                Command = new Command<FileEvent>((e) =>
                {
                    var result = GlslCodeGenerator.TryToCompile(e);
                    if (result.Success) DebLogger.Info(result);
                    else DebLogger.Error(result);
                })
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            MainCanvas = this.FindControl<Canvas>("MainCanvas");
            ToolbarContainer = this.FindControl<Border>("ToolbarContainer");
            StatusBarContainer = this.FindControl<Border>("StatusBarContainer");
        }
        private void InitializeStatusBar()
        {
            _statusBar = new EditorStatusBar(StatusBarContainer);
            Status.RegisterStatusProvider(_statusBar);
            Status.SetStatus("Ready");
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ServiceHub.Dispose();
        }
    }
}
