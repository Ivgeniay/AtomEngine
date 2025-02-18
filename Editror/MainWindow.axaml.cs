using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections.ObjectModel;

namespace Editor
{
    public partial class MainWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging;
        private Border _currentWindow;
        private Vector _totalOffset;

        public MainWindow()
        {
            InitializeComponent();
            CreateWindow("Окно 1", 10, 10);
            CreateWindow("Окно 2", 220, 10);
        }

        private void CreateWindow(string title, double left, double top)
        {
            var window = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Width = 200,
                Height = 150
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Parse("30") });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Parse("*") });

            var titleBar = new Border
            {
                Background = Brushes.LightGray
            };

            var titleText = new TextBlock
            {
                Text = title,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(10, 0)
            };

            titleBar.Child = titleText;

            titleBar.PointerPressed += OnTitleBarPointerPressed;
            titleBar.PointerReleased += OnTitleBarPointerReleased;
            titleBar.PointerMoved += OnTitleBarPointerMoved;

            var content = new TextBlock
            {
                Text = $"Содержимое {title}",
                Margin = new Thickness(10)
            };

            Grid.SetRow(titleBar, 0);
            Grid.SetRow(content, 1);

            grid.Children.Add(titleBar);
            grid.Children.Add(content);

            window.Child = grid;

            Canvas.SetLeft(window, left);
            Canvas.SetTop(window, top);

            MainCanvas.Children.Add(window);
        }

        private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var titleBar = sender as Border;
                _currentWindow = titleBar.Parent.Parent as Border;
                _startPoint = e.GetPosition(this);
                _isDragging = true;
                _totalOffset = new Vector(Canvas.GetLeft(_currentWindow), Canvas.GetTop(_currentWindow));
                titleBar.Cursor = new Cursor(StandardCursorType.DragMove);
                e.Pointer.Capture(titleBar);
            }
        }

        private void OnTitleBarPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var titleBar = sender as Border;
            _isDragging = false;
            _currentWindow = null;
            titleBar.Cursor = new Cursor(StandardCursorType.Arrow);
            e.Pointer.Capture(null);
        }

        private void OnTitleBarPointerMoved(object sender, PointerEventArgs e)
        {
            if (!_isDragging || _currentWindow == null)
                return;

            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _startPoint;
            var newOffset = _totalOffset + delta;

            // Проверяем границы
            if (newOffset.X < 0) newOffset = newOffset.WithX(0);
            if (newOffset.Y < 0) newOffset = newOffset.WithY(0);
            if (newOffset.X + _currentWindow.Width > Bounds.Width)
                newOffset = newOffset.WithX(Bounds.Width - _currentWindow.Width);
            if (newOffset.Y + _currentWindow.Height > Bounds.Height)
                newOffset = newOffset.WithY(Bounds.Height - _currentWindow.Height);

            Canvas.SetLeft(_currentWindow, newOffset.X);
            Canvas.SetTop(_currentWindow, newOffset.Y);
        }
    }

    public class DraggableWindow : AvaloniaObject
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<DraggableWindow, string>(nameof(Title));

        public static readonly StyledProperty<double> WidthProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Width));

        public static readonly StyledProperty<double> HeightProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Height));

        public static readonly StyledProperty<double> LeftProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Left));

        public static readonly StyledProperty<double> TopProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Top));

        public static readonly StyledProperty<string> ContentProperty =
            AvaloniaProperty.Register<DraggableWindow, string>(nameof(Content));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public double Width
        {
            get => GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public double Height
        {
            get => GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public double Left
        {
            get => GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }

        public double Top
        {
            get => GetValue(TopProperty);
            set => SetValue(TopProperty, value);
        }

        public string Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
    }
}
