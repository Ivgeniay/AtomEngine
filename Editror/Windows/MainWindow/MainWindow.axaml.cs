﻿using System.Collections.Generic;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using AtomEngine;
using EngineLib;
using Avalonia;
using System;
using OpenglLib;
using System.Numerics;
using static AtomEngine.AtomMath;
using Avalonia.Media;
using Avalonia.Threading;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace Editor
{
#if FEATURE
#endif
    public partial class MainWindow : Window
    {
        public static Canvas MainCanvas_ { get; private set; }
        private MainWindowUIManager _uIManager;
        private SceneManager _sceneManager;
        private ReloaderAssemblyData _reloader;

        private EditorToolbar _toolbar;
        private EditorStatusBar _statusBar;
        public MainWindow()
        {
            SystemDecorations = SystemDecorations.Full;
#if DEBUG
            this.AttachDevTools();
#endif

            InitializeComponent();
            InitializeStatusBar();

            Input.Initialize(new EditorInputSystem(this));

            MainCanvas_ = MainCanvas;
            ServiceHub.Get<DraggableWindowManagerService>().SetCanvas(MainCanvas);
            ServiceHub.Get<LoadingManager>().SetCanvas(MainCanvas);
            ServiceHub.Get<BuildManager>().SetMainWindow(this);
            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.SetMainWindow(this);

            AssetDependencyManager assetDependencyManager = ServiceHub.Get<AssetDependencyManager>();
            assetDependencyManager.RegisterDependencyHandler(MetadataType.Material, new MaterialComponentDependencyHandler());
            assetDependencyManager.RegisterDependencyHandler(MetadataType.Model, new MeshComponentDependencyHandler());
            assetDependencyManager.RegisterDependencyHandler(MetadataType.Shader, new MaterialShaderDependencyHandler());

            _uIManager = new MainWindowUIManager(this);

            var chat = new ChatController();
            var worlds = new WorldController();
            var console = new ConsoleController();
            var explorer = new ExplorerController();
            var hierarhy = new HierarchyController();
            var inspector = new InspectorController();
            var sceneView = new SceneViewController();
            var systems = new SystemDependencyController();
            var nodeGraphController = new NodeGraphController();
            var docController = new DocumentationController();
            var glslEditor = new GlslEditorController();

            var test = new GLTestController();

            _uIManager.RegisterController(MainControllers.Hierarchy, hierarhy);
            _uIManager.RegisterController(MainControllers.Inspector, inspector);
            _uIManager.RegisterController(MainControllers.World, worlds);
            _uIManager.RegisterController(MainControllers.Console, console);
            _uIManager.RegisterController(MainControllers.Explorer, explorer);
            _uIManager.RegisterController(MainControllers.SceneRender, sceneView);
            _uIManager.RegisterController(MainControllers.Chat, chat);
            _uIManager.RegisterController(MainControllers.Systems, systems);
            _uIManager.RegisterController(MainControllers.Graph, nodeGraphController);
            _uIManager.RegisterController(MainControllers.Documentation, docController);
            _uIManager.RegisterController(MainControllers.GlslEditor, glslEditor);
            _uIManager.RegisterController(MainControllers.Test, test);
            
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
                        Description = "System manager",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Systems);
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
                        Text = "Chat",
                        Description = "Ai chat",
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Chat);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Graph",
                        Description = "Test graph",
                        Action = () =>
                        {
                            _uIManager.OpenWindow(MainControllers.Graph);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "GlslEditor",
                        Description = "Glsl editor",
                        Action = () =>
                        {
                            _uIManager.OpenWindow(MainControllers.GlslEditor);
                        }
                    }

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
                        Text = "Compile Scripts",
                        Description = "",
                        Action = async () => {
                            DebLogger.Debug("Build Project Scripts");
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
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Build Product",
                        Description = "Build product",
                        Action = async () =>
                        {
                            BuildConfig config = new BuildConfig();
                            await ServiceHub.Get<BuildManager>().BuildProject(config);
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
                        Action = () => {
                            _uIManager.OpenWindow(MainControllers.Documentation);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "About",
                        Description = "",
                        Action = () => { DebLogger.Debug("About"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Test",
                        Description = "",
                        Action = () => { _uIManager.OpenWindow(MainControllers.Test); }
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

            _reloader = new ReloaderAssemblyData();
            _reloader.RegisterCacheble(inspector);
            _reloader.RegisterCacheble(hierarhy);
            _reloader.RegisterCacheble(worlds);
            _reloader.RegisterCacheble(systems);

            ServiceHub.Get<FileSystemWatcher>().RegisterCommand(new FileEventCommand()
            {
                FileExtension = ".glsl",
                Type = FileEventType.FileChanged,
                Command = new Command<FileEvent>((e) =>
                {
                    var result = GlslCompiler.TryToCompile(e);
                    if (result.Success) DebLogger.Info(result);
                    else DebLogger.Error(result);
                })
            });

            EditorUpdateSystem.RenderStart();
            //EditorUpdateSystem.UpdateDataStart();
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
            _uIManager.Dispose();
            ServiceHub.Dispose();
        }
    }

    public static class EditorUpdateSystem
    {
        private static readonly DispatcherTimer _renderTimer;
        private static readonly DispatcherTimer _dataUpdateTimer;
        private static bool _needsSceneUpdate = false;
        private static bool _needsDataUpdate = false;
        private static readonly List<IComponentObserver> _componentObservers = new List<IComponentObserver>();

        public static event EventHandler? SceneUpdateRequested;
        public static event EventHandler? DataUpdateRequested;

        static EditorUpdateSystem()
        {
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0 / 50)
            };

            _dataUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0 / 2)
            };

            _renderTimer.Tick += OnRenderTimerTick;
            _dataUpdateTimer.Tick += OnDataUpdateTick;
        }

        public static double UpdateFrequency
        {
            get => 1.0 / _renderTimer.Interval.TotalSeconds;
            set => _renderTimer.Interval = TimeSpan.FromSeconds(1.0 / value);
        }

        public static void RenderStart() => _renderTimer.Start();
        public static void UpdateDataStart() => _dataUpdateTimer.Start();
        public static void RenderStop() => _renderTimer.Stop();
        public static void UpdateDataStop() => _dataUpdateTimer.Stop();
        public static bool IsRenderRunning => _renderTimer.IsEnabled;

        public static void RequestSceneUpdate()
        {
            _needsSceneUpdate = true;
        }

        public static void RequestDataUpdate()
        {
            _needsDataUpdate = true;
        }

        public static void UpdateSceneNow()
        {
            SceneUpdateRequested?.Invoke(null, EventArgs.Empty);
        }

        public static void CheckComponentNow()
        {
            DataUpdateRequested?.Invoke(null, EventArgs.Empty);
        }

        public static void RegisterComponentObserver(IComponentObserver observer)
        {
            if (!_componentObservers.Contains(observer))
            {
                _componentObservers.Add(observer);
                observer.ComponentChanged += OnComponentChanged;
            }
        }

        public static void UnregisterComponentObserver(IComponentObserver observer)
        {
            if (_componentObservers.Contains(observer))
            {
                _componentObservers.Remove(observer);
                observer.ComponentChanged -= OnComponentChanged;
            }
        }

        public static void UnregisterAllObservers()
        {
            foreach (var observer in _componentObservers)
            {
                observer.ComponentChanged -= OnComponentChanged;
            }
            _componentObservers.Clear();
        }

        private static void OnComponentChanged(object? sender, ComponentChangedEventArgs e)
        {
            RequestDataUpdate();
        }

        private static void OnRenderTimerTick(object? sender, EventArgs e)
        {
            if (_needsSceneUpdate)
            {
                _needsSceneUpdate = false;

                EditorSetter.Invoke(() =>
                {
                    SceneUpdateRequested?.Invoke(null, EventArgs.Empty);
                });
            }
        }

        private static void OnDataUpdateTick(object? sender, EventArgs e)
        {
            //if (_needsDataUpdate)
            //{
                _needsDataUpdate = false;
                EditorSetter.Invoke(() =>
                {
                    DataUpdateRequested?.Invoke(null, EventArgs.Empty);
                });
            //}

            //CheckComponentChanges();
        }

        private static void CheckComponentChanges()
        {
            foreach (var observer in _componentObservers)
            {
                observer.CheckForChanges();
            }
        }

        public static void Dispose()
        {
            _componentObservers.Clear();
            _renderTimer.Stop();
        }
    }

    public interface IComponentObserver
    {
        void CheckForChanges();
        event EventHandler<ComponentChangedEventArgs> ComponentChanged;
    }

    public class ComponentChangedEventArgs : EventArgs
    {
        public object Component { get; }
        public string PropertyName { get; }

        public ComponentChangedEventArgs(object component, string propertyName)
        {
            Component = component;
            PropertyName = propertyName;
        }
    }

    public static class EditorSetter
    {
        public static void Invoke(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    action();
                });
            }
        }

        public static async Task InvokeAsync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                try
                {
                    action();
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    DebLogger.Error(ex);
                    await Task.FromException(ex);
                }
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(action);
            }
        }
        

        public static void Post(Action action, DispatcherPriority priority = default)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    action();
                }, priority);
            }
        }
    }
}
