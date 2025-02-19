using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System.Diagnostics;
using System;

namespace Editor
{
    public partial class MainWindow : Window
    {

        private DraggableWindowFactory _windowFactory;
        private EditorToolbar _toolbar;
        private EditorStatusBar _statusBar;

        public MainWindow()
        {
            InitializeComponent();

            // Добавляем стили для тулбара и статусбара
            //this.Styles.Add(AvaloniaXamlLoader.Load(new Uri("avares://Editor/Styles/Common.axaml")));

            // Инициализируем компоненты
            InitializeToolbar();
            InitializeStatusBar();
            InitializeWindowFactory();

            _windowFactory.CreateWindow("Окно 1", null, 10, 40);
            _windowFactory.CreateWindow("Окно 2", null, 220, 40);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Получаем ссылки на элементы интерфейса
            MainCanvas = this.FindControl<Canvas>("MainCanvas");
            ToolbarContainer = this.FindControl<Border>("ToolbarContainer");
            StatusBarContainer = this.FindControl<Border>("StatusBarContainer");
        }

        private void InitializeToolbar()
        {
            _toolbar = new EditorToolbar(ToolbarContainer, OnMenuItemClicked);
        }

        private void InitializeStatusBar()
        {
            _statusBar = new EditorStatusBar(StatusBarContainer);
            _statusBar.SetStatus("Ready");
        }

        private void InitializeWindowFactory()
        {
            _windowFactory = new DraggableWindowFactory(MainCanvas);
        }

        private void OnMenuItemClicked(string itemName)
        {
            _statusBar.SetStatus($"Selected: {itemName}");
            switch (itemName)
            {
                case "Project Explorer":
                    _windowFactory.CreateWindow("Project Explorer", null, 10, 40, 250, 400);
                    break;
                case "Properties":
                    _windowFactory.CreateWindow("Properties", null, 520, 40, 250, 400);
                    break;
                case "Console":
                    _windowFactory.CreateWindow("Console", null, 10, 320, 760, 200);
                    break;
                case "Output":
                    _windowFactory.CreateWindow("Output", null, 270, 40, 240, 270);
                    break;
                case "Exit":
                    Close();
                    break;
                default:
                    Debug.WriteLine($"Menu item clicked: {itemName}");
                    break;
            }
        }


        public Border CreateDraggableWindow(string title, Control content = null, double left = 10, double top = 10,
            double width = 200, double height = 150) =>
            _windowFactory.CreateWindow(title, content, left, top, width, height);
    }


}
