using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;

namespace Editor
{
    public partial class MainWindow : Window
    {
        private DraggableWindowFactory _windowFactory;
        private EditorToolbar _toolbar;
        private EditorStatusBar _statusBar;

        private ConsoleController _consoleController;
        private OpenGlController _openGlController;
        private HierarchyController _hierarchyController;
        private WorldController _worldController;

        private Scene _currentScene;

        public MainWindow()
        {
            SystemDecorations = SystemDecorations.Full;
            //ExtendClientAreaToDecorationsHint = true;
            //ExtendClientAreaTitleBarHeightHint = 30;
            InitializeComponent();

            InitializeToolbar();
            InitializeStatusBar();
            InitializeConsole();
            InitializeWindowFactory();

            HandleNewScene().GetAwaiter().GetResult();
            InitializeHierarchy();
            WorldInitializr();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            MainCanvas = this.FindControl<Canvas>("MainCanvas");
            ToolbarContainer = this.FindControl<Border>("ToolbarContainer");
            StatusBarContainer = this.FindControl<Border>("StatusBarContainer");

            Select.OnSelect += (e) =>
            {
                if (Select.Selected.Count() > 1) Status.SetStatus($"Selected {Select.Selected.Count()} entities");
                else Status.SetStatus($"Selected {e.Name}");
            };
            Select.OnDeSelect += (e) =>
            {
                if (Select.Selected.Count() > 1) Status.SetStatus($"Selected {Select.Selected.Count()} entities");
                else if (Select.Selected.Count() == 1) Status.SetStatus($"Selected {Select.Selected.First().Name}");
                else Status.SetStatus("No enityt selected");
            };
        }

        private void InitializeToolbar()
        {
            _toolbar = new EditorToolbar(ToolbarContainer);

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
                            await HandleNewScene();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Open",
                        Description = "Open scene from drive",
                        Action = async () =>
                        {
                            await HandleOpenScene();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Save",
                        Description = "Save current scene",
                        Action = async () =>
                        {
                            await HandleSaveScene();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Save As...",
                        Description = "Save current scene as...",
                        Action = async () =>
                        {
                            await HandleSaveSceneAs();
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
                            DraggableWindow window = _windowFactory?.CreateWindow("Hierarchy", _hierarchyController, 10, 40, 250, 400);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Inspector",
                        Description = "",
                        Action = () => { _windowFactory.CreateWindow("Inspector", null, 520, 40, 250, 400); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "World",
                        Description = "",
                        Action = () => { 
                            DraggableWindow window = _windowFactory?.CreateWindow("Worlds", _worldController, 10, 40, 250, 400);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Systems",
                        Description = "",
                        Action = () => { DebLogger.Debug("Systems"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Scene",
                        Description = "",
                        Action = () => {
                            if (_openGlController == null) _openGlController = new OpenGlController();

                            _openGlController.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                            _openGlController.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

                            var sceneWindow = _windowFactory.CreateWindow("Scene", _openGlController, 270, 320, 500, 200);
                            sceneWindow.OnClose += (sender) => _openGlController.Dispose();
                            _openGlController = null;
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Game",
                        Description = "",
                        Action = () => { _windowFactory.CreateWindow("Game", null, 10, 320, 760, 200); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Console",
                        Description = "",
                        Action = () => {
                            var consoleWindow = _windowFactory?.CreateWindow("Console", _consoleController, 10, 320, 760, 250);
                            DebLogger.AddLogger(_consoleController);
                            consoleWindow.OnClose += (sender) => DebLogger.RemoveLogger(_consoleController);
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Output",
                        Description = "",
                        Action = () => { DebLogger.Debug("Undo"); }
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
                        Action = () => { DebLogger.Debug("Build Project"); }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Build Solution",
                        Description = "",
                        Action = () => { DebLogger.Debug("Build Solution"); }
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
                        Action = () => { DebLogger.Debug("Rebuild All"); }
                    },
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
        }

        private void InitializeStatusBar()
        {
            _statusBar = new EditorStatusBar(StatusBarContainer);
            Status.SetStatus("Ready");
        }

        private void InitializeWindowFactory()
        {
            _windowFactory = new DraggableWindowFactory(MainCanvas);
        }
        
        private void InitializeHierarchy()
        {
            _hierarchyController = new HierarchyController();

            _hierarchyController.EntityCreated += (s, entity) =>
            {
                //_currentScene.
                Status.SetStatus($"Created entity: {entity.Name} (ID: {entity.Id})");
            };

            _hierarchyController.EntityRenamed += (s, entity) =>
                Status.SetStatus($"Renamed entity to: {entity.Name}");

            _hierarchyController.EntityDeleted += (s, entity) =>
                Status.SetStatus($"Deleted entity: {entity.Name}");

            UpdateHyerarchy();
        }

        private void InitializeConsole()
        {
            _consoleController = new ConsoleController();
        }

        private void WorldInitializr()
        {
            _worldController = new WorldController(_currentScene);
        }

        public Border CreateDraggableWindow(string title, Control content = null, double left = 10, double top = 10,
            double width = 200, double height = 150) =>
            _windowFactory.CreateWindow(title, content, left, top, width, height);


        /// <summary>
        /// Обрабатывает создание новой сцены
        /// </summary>
        private async Task HandleNewScene()
        {
            if (_currentScene != null && _currentScene.IsDirty)
            {
                var res = await ConfirmationDialog.Show(
                    this,
                    "Worning",
                    "There are scene changes. Do you want to safe scene?",
                    true);

                switch (res)
                {
                    case ConfirmationDialog.DialogResult.Cancel:
                        return;
                    case ConfirmationDialog.DialogResult.Yes:
                        var t = await FileDialogService.SaveFileAsync(
                            this,
                            $"Safe {_currentScene.WorldName}",
                            $"{_currentScene.WorldName}",
                            new FileDialogService.FileFilter("scene", "scene"));

                        if (t != null) Status.SetStatus($"{t}");
                        break;

                    case ConfirmationDialog.DialogResult.No:
                        break;
                }
            }

            // Сбрасываем иерархию
            _hierarchyController?.ClearEntities();
            WorldData standartSceneData = SceneFileHelper.CreateNewScene();
            _currentScene = new Scene(new List<WorldData>() { standartSceneData }, standartSceneData);
            UpdateHyerarchy();
            Status.SetStatus($"Created new scene: {_currentScene.WorldName}");
        }

        /// <summary>
        /// Обрабатывает открытие сцены
        /// </summary>
        private async Task HandleOpenScene()
        {
            if (_currentScene != null && _currentScene.IsDirty)
            {
                var res = await ConfirmationDialog.Show(
                    this,
                    "Worning",
                    "There are scene changes. Do you want to safe scene?",
                    true);

                switch (res)
                {
                    case ConfirmationDialog.DialogResult.Cancel:
                        return;
                    case ConfirmationDialog.DialogResult.Yes:
                        var t = await FileDialogService.SaveFileAsync(
                            this,
                            $"Safe {_currentScene.WorldName}",
                            $"{_currentScene.WorldName}",
                            new FileDialogService.FileFilter("scene", "scene"));

                        if (t != null) Status.SetStatus($"{t}");
                        break;

                    case ConfirmationDialog.DialogResult.No:
                        break;
                }
            }
            var loadedScene = await SceneFileHelper.OpenSceneAsync(this);
            if (loadedScene != null)
            {
                _currentScene = loadedScene;

                if (_hierarchyController != null)
                    UpdateHyerarchy();
                Status.SetStatus($"Opened scene: {_currentScene.WorldName}");
            }
            else
            {
                Status.SetStatus($"Opening scene failed");
            }
        }

        /// <summary>
        /// Обрабатывает сохранение текущей сцены
        /// </summary>
        private async Task HandleSaveScene()
        {
            if (string.IsNullOrEmpty(_currentScene.ScenePath))
            {
                await HandleSaveSceneAs();
            }
            else
            {
                string jsonContent = JsonConvert.SerializeObject(_currentScene);
                bool result = await FileDialogService.WriteTextFileAsync(_currentScene.ScenePath, jsonContent);
                if (result) Status.SetStatus($"Save scene succesful");
                else Status.SetStatus($"Save scene not succesful");
            }
        }

        /// <summary>
        /// Обрабатывает сохранение сцены с выбором имени файла
        /// </summary>
        private async Task HandleSaveSceneAs()
        {
            var result = await SceneFileHelper.SaveSceneAsync(this, _currentScene);
            if (result.Item1)
            {
                Status.SetStatus($"Scene saved: {result.Item2}");
            }
            else
            {
                Status.SetStatus("Failed to save scene");
            }
        }

        private void UpdateHyerarchy()
        {
            if (_hierarchyController != null)
            {
                _hierarchyController.ClearEntities();
                foreach (var entity in _currentScene.CurrentWorldData.Entities)
                {
                    _hierarchyController.CreateNewEntity(entity.Name);
                }
            }
        }
    }


}
