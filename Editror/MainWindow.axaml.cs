using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;
using Editor.Utils.Generator;
using System.IO;

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
        private InspectorController _inspectorController;
        private DirectoryExplorerController _directoryExplorerController;

        private ProjectScene _currentScene;

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
            InitializeWorld();
            InitializeInspector();
            InitializeExplorer();

            UpdateControllers();
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
                            _hierarchyController.Open();
                            window.OnClose += (e) => _hierarchyController.Close();
                        }
                    },
                    new EditorToolbarButton()
                    {
                        Text = "Inspector",
                        Description = "",
                        Action = () => { 
                            var window = _windowFactory.CreateWindow("Inspector", _inspectorController, 520, 40, 250, 400);
                            _inspectorController.Open();
                            window.OnClose += (e) => _inspectorController.Close();
                        }
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
                        Text = "Project",
                        Description = "Project file explorer",
                        Action = () => {
                            var window = _windowFactory.CreateWindow("Project", _directoryExplorerController, 10, 320, 760, 250);

                            _directoryExplorerController.FileSelected += (path) => {
                                Status.SetStatus($"Selected file: {path}");
                            };

                            _directoryExplorerController.DirectorySelected += (path) => {
                                Status.SetStatus($"Selected directory: {path}");
                            };
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

            _hierarchyController.EntityCreated += (s, entityName) =>
            {
                Select.DeSelectAll();
                _currentScene.AddEntity(entityName);
                UpdateHyerarchy();
                CleanInspector();
                Status.SetStatus($"Created entity: {entityName}");
            };

            _hierarchyController.EntityDuplicated += (s, entityName) =>
            {
                Select.DeSelectAll();
                _currentScene.AddDuplicateEntity(entityName);
                UpdateHyerarchy();
                CleanInspector();
                Status.SetStatus($"Duplicate entity");
            };

            _hierarchyController.EntityRenamed += (s, entity) =>
            {
                Select.DeSelectAll();
                _currentScene.RenameEntity(entity);
                CleanInspector();
                Status.SetStatus($"Renamed entity to: {entity.Name}");
            };

            _hierarchyController.EntityDeleted += (s, entity) =>
            {
                Select.DeSelectAll();
                _currentScene.DeleteEntity(entity);
                CleanInspector();
                Status.SetStatus($"Deleted entity: {entity.Name}");
            };

            _hierarchyController.EntitySelected += (s, entity) =>
            {
                IInspectable inspectable = InspectorDistributor.GetInspectable(entity);
                if (inspectable != null) _inspectorController.Inspect(inspectable);
                else _inspectorController.CleanInspected();
            };
        }

        private void InitializeConsole()
        {
            _consoleController = new ConsoleController();

            _consoleController.RegisterConsoleCommand(
                new ConsoleCommand
                {
                    CommandName = "exclude",
                    Description = "Exclude extension file from explorer browser",
                    Action = (e) =>
                    {
                        _directoryExplorerController.ExcludeFileExtension(e);
                    }
                });
        }

        private void InitializeWorld()
        {
            _worldController = new WorldController(_currentScene);
            _worldController.WorldRenamed += (sender, e) =>
            {
                Select.DeSelectAll();
                _currentScene.RenameWorld(e);
            };
            _worldController.WorldDeleted += (sender, e) =>
            {
                Select.DeSelectAll();
                _currentScene.RemoveWorld(e);
            };
            _worldController.WorldCreated += (sender, e) =>
            {
                Select.DeSelectAll();
                _currentScene.CreateWorld(e);
            };
            _worldController.WorldSelected += (sender, e) =>
            {
                Select.DeSelectAll();
                _currentScene.SelecteWorld(e);
                UpdateHyerarchy();
            };
        }

        private void InitializeInspector()
        {
            _inspectorController = new InspectorController();
        }

        private void InitializeExplorer()
        {
            ExplorerConfigurations configurations = ServiceHub.Get<Configuration>().GetConfiguration<ExplorerConfigurations>(ConfigurationSource.ExplorerConfigs);
            _directoryExplorerController = new DirectoryExplorerController(configurations);

            _directoryExplorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".txt",
                Name = "Read",
                Description = "sd",
                Action = (e) => DebLogger.Debug($"{e}"),
                SubCategory = new string[] { "sub1", "sub2" }
            });
            _directoryExplorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".cs",
                Name = "Open in IDE",
                Description = "Open file in IDE",
                Action = (e) =>
                {
                    ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE(e.FileFullPath);
                },
            });
            _directoryExplorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Open in IDE",
                Description = "Open file in IDE",
                Action = (e) =>
                {
                    ServiceHub.Get<ScriptSyncSystem>().OpenProjectInIDE(e.FileFullPath);
                },
            });
            _directoryExplorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".glsl",
                Name = "Generate C#",
                Description = "Generate c sharp view glsl code",
                Action = (e) =>
                {
                    GlslCodeGenerator.GenerateCode(e.FileFullPath, e.FilePath);
                }
            });

            _directoryExplorerController.RegisterCustomContextMenu(new DescriptionCustomContextMenu
            {
                Extension = ".cs",
                Name = "Create Material",
                Description = "Create material from shader representation",
                Action = (e) =>
                {
                    // Проверяем, что это сгенерированный файл представления шейдера
                    if (e.FileName.EndsWith("Representation.g.cs"))
                    {
                        // Получаем метаданные файла
                        var metadata = MetadataManager.Instance.GetMetadata(e.FileFullPath);

                        // Создаем материал
                        var materialController = new MaterialEditorController();
                        var material = materialController.CreateMaterial(metadata.Guid);

                        // Определяем путь для сохранения материала
                        string materialPath = Path.Combine(
                            Path.GetDirectoryName(e.FileFullPath),
                            $"{Path.GetFileNameWithoutExtension(e.FileName).Replace("Representation.g", "")}_Material.mat"
                        );

                        // Сохраняем материал
                        materialController.SaveMaterial(material, materialPath);

                        // Открываем материал для редактирования
                        //materialController.SetCurrentMaterial(material);

                        Status.SetStatus($"Created material: {material.Name}");
                    }
                }
            });

            _directoryExplorerController.FileSelected += (fileData) =>
            {
                IInspectable inspectable = InspectorDistributor.GetInspectable(fileData);
                if (inspectable != null) _inspectorController.Inspect(inspectable);
                else _inspectorController.CleanInspected();
            };
        }

        public Border CreateDraggableWindow(string title, Control content = null, double left = 10, double top = 10,
            double width = 200, double height = 150) =>
            _windowFactory.CreateWindow(title, content, left, top, width, height);


        /// <summary>
        /// Обрабатывает создание новой сцены
        /// </summary>
        private async Task HandleNewScene()
        {
            if (_currentScene !=null && _currentScene.IsDirty)
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

            CleanInspector();
            CleanHyerarchy();
            CleanWorlds();
            WorldData1 standartSceneData = SceneFileHelper.CreateNewScene();
            _currentScene = new ProjectScene(new List<WorldData1>() { standartSceneData }, standartSceneData);
            InspectorDistributor.Initialize(_currentScene);
            UpdateControllers();
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
                CleanInspector();
                CleanHyerarchy();
                CleanWorlds();
                _currentScene = loadedScene;
                UpdateControllers();
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
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                };

                string jsonContent = JsonConvert.SerializeObject(_currentScene, jsonSettings);
                bool result = await FileDialogService.WriteTextFileAsync(_currentScene.ScenePath, jsonContent);
                if (result)
                {
                    _currentScene.MakeUndirty();
                    Status.SetStatus($"Save scene succesful");
                }
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
                _currentScene.MakeUndirty();
                Status.SetStatus($"Scene saved: {result.Item2}");
            }
            else
            {
                Status.SetStatus("Failed to save scene");
            }
        }

        private void UpdateControllers()
        {
            UpdateHyerarchy();
            UpdateWorlds();
            CleanInspector();
        }

        private void UpdateHyerarchy() => _hierarchyController?.UpdateHyerarchy(_currentScene);
        private void CleanHyerarchy() => _hierarchyController?.ClearEntities();

        private void UpdateWorlds() => _worldController?.UpdateWorlds(_currentScene);
        private void CleanWorlds() => _worldController?.ClearWorlds();

        private void CleanInspector() => _inspectorController?.CleanInspected();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ServiceHub.Dispose();
            //CodeFilesSynchronizer.Dispose();
            //AssetFileSystem.Instance.Dispose();
            //ProjectFileWatcher.Dispose();
        }
    }


}
