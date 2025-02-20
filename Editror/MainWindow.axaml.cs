using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;
using System.Threading.Tasks;

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

        private Scene _currentScene;

        public MainWindow()
        {
            SystemDecorations = SystemDecorations.Full;
            //ExtendClientAreaToDecorationsHint = true;
            //ExtendClientAreaTitleBarHeightHint = 30;
            InitializeComponent();

            InitializeToolbar();
            InitializeStatusBar();
            InitializeWindowFactory();

            _currentScene = new Scene();
            _currentScene.SceneData = new SceneData()
            {
                SceneName = "Default",
                Entities = new System.Collections.Generic.List<EntityData>()
                {
                    new EntityData() { Id = 0, Version = 0, Name = "Main Camera", },
                    new EntityData() { Id = 1, Version = 0, Name = "Directional Light", },
                    new EntityData() { Id = 2, Version = 0, Name = "Player", },
                    new EntityData() { Id = 3, Version = 0, Name = "Environment", },
                }
            };

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
            _toolbar = new EditorToolbar(ToolbarContainer, OnMenuItemClicked);
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
        
        private async void OnMenuItemClicked(string itemName)
        {
            _statusBar.SetStatus($"Selected: {itemName}");
            switch (itemName)
            {
                case "New":
                    await HandleNewScene();
                    break;
                case "Open":
                    await HandleOpenScene();
                    break;
                case "Save":
                    await HandleSaveScene();
                    break;
                case "Save As...":
                    await HandleSaveSceneAs();
                    break;
                case "Hierarchy":
                    if (_hierarchyController == null)
                    {
                        _hierarchyController = new HierarchyController();

                        _hierarchyController.EntityCreated += (s, entity) =>
                            _statusBar?.SetStatus($"Created entity: {entity.Name} (ID: {entity.Id})");

                        _hierarchyController.EntityRenamed += (s, entity) =>
                            _statusBar?.SetStatus($"Renamed entity to: {entity.Name}");

                        _hierarchyController.EntityDeleted += (s, entity) =>
                            _statusBar?.SetStatus($"Deleted entity: {entity.Name}");

                        // Создаем стандартные сущности
                        foreach(var entity in _currentScene.SceneData.Entities)
                        {
                            _hierarchyController.CreateNewEntity(entity.Name);
                        }
                    }

                    _windowFactory?.CreateWindow("Hierarchy", _hierarchyController, 10, 40, 250, 400);
                    break;
                case "Inspector":
                    _windowFactory.CreateWindow("Inspector", null, 520, 40, 250, 400);
                    break;
                case "Console":
                    if (_consoleController == null) _consoleController = new ConsoleController();
                    var consoleWindow = _windowFactory?.CreateWindow("Console", _consoleController, 10, 320, 760, 250);
                    DebLogger.AddLogger(_consoleController);
                    consoleWindow.OnClose += (sender) => DebLogger.RemoveLogger(_consoleController);
                    break;
                case "Output":
                    _windowFactory.CreateWindow("Output", null, 270, 40, 240, 270);
                    break;
                case "Scene":
                    if (_openGlController == null) _openGlController = new OpenGlController();

                    _openGlController.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                    _openGlController.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

                    var sceneWindow = _windowFactory.CreateWindow("Scene", _openGlController, 270, 320, 500, 200);
                    sceneWindow.OnClose += (sender) => _openGlController.Dispose();
                    _openGlController = null;
                    break;

                case "Game":
                    _windowFactory.CreateWindow("Game", null, 10, 320, 760, 200);
                    break;
                case "Exit":
                    Close();
                    break;
                default:
                    DebLogger.Debug($"Menu item clicked: {itemName}");
                    break;
            }
        }


        public Border CreateDraggableWindow(string title, Control content = null, double left = 10, double top = 10,
            double width = 200, double height = 150) =>
            _windowFactory.CreateWindow(title, content, left, top, width, height);


        /// <summary>
        /// Обрабатывает создание новой сцены
        /// </summary>
        private async Task HandleNewScene()
        {
            // Если есть несохраненные изменения, запрашиваем подтверждение
            //if (_currentScene != null)
            //{
            //    // TODO: В будущем добавить диалог подтверждения
            //}

            //_currentScene = SceneFileHelper.CreateNewScene();
            //Status.SetStatus($"Created new scene: {_currentScene.SceneName}");

            // Сбрасываем иерархию
            //if (_hierarchyController != null)
            //{
            //    _hierarchyController.ClearEntities();

            //    // Добавляем стандартные сущности из новой сцены
            //    foreach (var entity in _currentScene.Entities)
            //    {
            //        _hierarchyController.CreateNewEntity(entity.Name);
            //    }
            //}
        }

        /// <summary>
        /// Обрабатывает открытие сцены
        /// </summary>
        private async Task HandleOpenScene()
        {
            //var loadedScene = await SceneFileHelper.OpenSceneAsync(this);
            //if (loadedScene != null)
            //{
            //    _currentScene = loadedScene;
            //    Status.SetStatus($"Opened scene: {_currentScene.SceneName}");

            //    // Обновляем иерархию, если окно Hierarchy открыто
            //    if (_hierarchyController != null)
            //    {
            //        _hierarchyController.ClearEntities();

            //        foreach (var entity in _currentScene.Entities)
            //        {
            //            _hierarchyController.CreateNewEntity(entity.Name);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Обрабатывает сохранение текущей сцены
        /// </summary>
        private async Task HandleSaveScene()
        {
            //if (_currentScene == null)
            //{
            //    // Если нет текущей сцены, создаем новую
            //    _currentScene = SceneFileHelper.CreateNewScene();
            //}

            //var success = await SceneFileHelper.SaveSceneAsync(this, _currentScene);
            bool success = true;
            if (success)
            {
                Status.SetStatus($"Scene saved: ");
            }
            else
            {
                Status.SetStatus("Failed to save scene");
            }
        }

        /// <summary>
        /// Обрабатывает сохранение сцены с выбором имени файла
        /// </summary>
        private async Task HandleSaveSceneAs()
        {
            //if (_currentScene == null)
            //{
            //    // Если нет текущей сцены, создаем новую
            //    _currentScene = SceneFileHelper.CreateNewScene();
            //}

            //var success = await SceneFileHelper.SaveSceneAsync(this, _currentScene);
            bool success = true;
            if (success)
            {
                Status.SetStatus($"Scene saved as: ");
            }
            else
            {
                Status.SetStatus("Failed to save scene");
            }
        }

        /// <summary>
        /// Обновляет SceneData на основе текущей иерархии
        /// </summary>
        private void UpdateSceneDataFromHierarchy()
        {
            //if (_currentScene == null || _hierarchyController == null)
            //    return;


            // TODO: Реализовать полное обновление данных сцены из иерархии
        }
    }


}
